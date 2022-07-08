﻿// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2022. All rights reserved.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using ArmoniK.DevelopmentKit.Common.Exceptions;
using ArmoniK.DevelopmentKit.SymphonyApi;
using ArmoniK.DevelopmentKit.SymphonyApi.api;
using ArmoniK.EndToEndTests.Common;
using Microsoft.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Tests.CheckSubtaskingTree_SymphonySDK
{
    public class ServiceContainer : ServiceContainerBase
    {
        public override void OnCreateService(ServiceContext serviceContext)
        {
            //END USER PLEASE FIXME
        }

        public override void OnSessionEnter(SessionContext sessionContext)
        {
            //END USER PLEASE FIXME
        }

        public void ChheckPayload(ClientPayload payload)
        {
            if (payload == null)
            {
                throw new ArgumentException($"ChheckPayload: payload must not be null");
            }
            if (payload.Type != ClientPayload.TaskType.Aggregation)
            {
                if (payload.Numbers == null)
                {
                    throw new ArgumentException($"ChheckPayload: payload numbers must not be null");
                }
                if (payload.NbSubTasks <= 1)
                {
                    throw new ArgumentException($"ChheckPayload: payload.NbSubTasks:{payload.NbSubTasks} must be >= 2");
                }
            }
        }

        public override byte[] OnInvoke(SessionContext sessionContext, TaskContext taskContext)
        {
            var payload = ClientPayload.Deserialize(taskContext.TaskInput);
            ChheckPayload(payload);

            if (payload.Type == ClientPayload.TaskType.None)
            {
                Logger.LogInformation($"OnInvoke payload:{payload.Type} numbers:[{string.Join(";", payload.Numbers ?? new List<int>())}] will be splitted in :{payload.NbSubTasks}");
                return SplitAndSum(taskContext, payload);
            }
            else if (payload.Type == ClientPayload.TaskType.Aggregation)
            {
                return AggregateValues(taskContext, payload);
            }
            return null;
        }


        public override void OnSessionLeave(SessionContext sessionContext)
        {
            //END USER PLEASE FIXME
        }

        public override void OnDestroyService(ServiceContext serviceContext)
        {
            //END USER PLEASE FIXME
        }


        private static IEnumerable<T[]> SplitList<T>(List<T> listToSplit, int nbSplit = 2)
        {
#if NET6_0_OR_GREATER
            return listToSplit.Chunk((int)Math.Ceiling(((decimal)listToSplit.Count / (decimal)nbSplit)));
#else
            int nbElemToTake = (int)Math.Ceiling((decimal)listToSplit.Count / (decimal)nbSplit);
            List<T> tempList = listToSplit;
            while (tempList.Count() > nbElemToTake)
            {
                yield return tempList.Take(nbElemToTake).ToArray();
                tempList = tempList.Skip(nbElemToTake).ToList();
            }
            yield return tempList.ToArray();
#endif
        }

        private byte[] SplitAndSum(TaskContext taskContext, ClientPayload clientPayload)
        {
            Logger.LogInformation($"Enter in function : SplitAndSum with taskId {taskContext.TaskId}");

            if (clientPayload.Numbers.Count == 0)
                return new ClientPayload
                {
                    Type = ClientPayload.TaskType.Result,
                    Result = 0,
                }.Serialize(); // Nothing to do

            if (clientPayload.Numbers.Count == 1)
            {
                var value = clientPayload.Numbers[0];
                Logger.LogInformation($"final tree {value} with taskId {taskContext.TaskId}");

                return new ClientPayload
                {
                    Type = ClientPayload.TaskType.Result,
                    Result = value,
                }
                  .Serialize();
            }
            else // if (clientPayload.numbers.Count > 1)
            {
                var splittedLists = SplitList(clientPayload.Numbers, clientPayload.NbSubTasks);

                List<string> subTaskIds = new List<string>(clientPayload.NbSubTasks);
                foreach (var splittedList in splittedLists)
                {
                    var childPayload = new ClientPayload
                    {
                        Type = clientPayload.Type,
                        Numbers = splittedList.ToList(),
                        NbSubTasks = clientPayload.NbSubTasks,
                        Result = 0,
                    };
                    if (splittedList.Count() <= 100)
                    {
                        Logger.LogInformation($"Submitting subTask from task          : {taskContext.TaskId} from Session {SessionId.Id} numbers : [{string.Join(';', splittedList)}] {clientPayload.Type}");
                    }
                    else
                    {
                        Logger.LogInformation($"Submitting subTask from task          : {taskContext.TaskId} from Session {SessionId.Id} numbers : [{string.Join(';', splittedList.Take(50))};...;{string.Join(';', splittedList.TakeLast(50))}] {clientPayload.Type}");
                    }
                    string subTaskId = this.SubmitTask(childPayload.Serialize());
                    subTaskIds.Add(subTaskId);
                }

                ClientPayload aggPayload = new()
                {
                    Type = ClientPayload.TaskType.Aggregation,
                    Result = 0,
                };

                Logger.LogInformation($"Submitting aggregation task             : {taskContext.TaskId} from Session {SessionId.Id}");
                var aggTaskId = this.SubmitTaskWithDependencies(aggPayload.Serialize(), subTaskIds, true);
                Logger.LogInformation($"Submitted  SubmitTaskWithDependencies : {aggTaskId} with task dependencies      {string.Join(';', subTaskIds.Take(10))}...");
                return null; //nothing to do
            }
        }

        private byte[] AggregateValues(TaskContext taskContext, ClientPayload clientPayload)
        {
            Logger.LogInformation($"Aggregate Task {taskContext.TaskId} request result from {taskContext.DependenciesTaskIds} Dependencies TaskIds : [{string.Join(", ", taskContext.DependenciesTaskIds.Take(10))}]");
            int aggregatedValuesSum = 0;
            foreach (var taskDependency in taskContext.DataDependencies)
            {
                if (taskDependency.Value == null || taskDependency.Value.Length == 0)
                    throw new WorkerApiException($"Cannot retrieve Result from taskId {taskContext.DependenciesTaskIds?.Single()}");
                var dependencyResultPayload = ClientPayload.Deserialize(taskDependency.Value);
                aggregatedValuesSum += dependencyResultPayload.Result;
            }
            Logger.LogInformation($"Aggregation taskId {taskContext.TaskId} has summed brothers, result = {aggregatedValuesSum}");
            aggregatedValuesSum += clientPayload.Result;

            ClientPayload aggregationResults = new()
            {
                Type = ClientPayload.TaskType.Result,
                Result = aggregatedValuesSum,
            };

            return aggregationResults.Serialize();
        }
    }
}
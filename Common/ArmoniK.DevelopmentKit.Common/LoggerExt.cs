﻿// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2022.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
//   J. Fonseca        <jfonseca@aneo.fr>
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.Logging;

using System.Reactive.Disposables;

#pragma warning disable CS1591

namespace ArmoniK.DevelopmentKit.Common;

public static class LoggerExt
{
  public static IDisposable BeginNamedScope(this ILogger                        logger,
                                            string                              name,
                                            params ValueTuple<string, object>[] properties)
  {
    var dictionary = properties.ToDictionary(p => p.Item1,
                                             p => p.Item2);
    dictionary[name + ".Scope"] = Guid.NewGuid();
    return logger.BeginScope(dictionary);
  }

  public static IDisposable BeginPropertyScope(this   ILogger                      logger,
                                               params ValueTuple<string, object>[] properties)
  {
    var dictionary = properties.ToDictionary(p => p.Item1,
                                             p => p.Item2);
    return logger.BeginScope(dictionary);
  }

  public static IDisposable LogFunction(this ILogger              logger,
                                        string                    id            = "",
                                        LogLevel                  level         = LogLevel.Trace,
                                        [CallerMemberName] string functionName  = "",
                                        [CallerFilePath]   string classFilePath = "",
                                        [CallerLineNumber] int    line          = 0)
  {
    if (!logger.IsEnabled(level))
    {
      return Disposable.Create(() => { });
    }

    var properties = new List<ValueTuple<string, object>>
    {
      (nameof(functionName), functionName),
      (nameof(classFilePath), classFilePath),
      (nameof(line), line),
    };
    if (!string.IsNullOrEmpty(id))
    {
      properties.Add(("Id", id));
    }

    var scope = logger.BeginNamedScope($"{classFilePath}.{functionName}",
                                       properties.ToArray());

    logger.Log(level,
               "Entering {classFilePath}.{functionName} - {Id}",
               classFilePath,
               functionName,
               id);

    return Disposable.Create(() =>
    {
      logger.Log(level,
                 "Leaving {classFilePath}.{functionName} - {Id}",
                 classFilePath,
                 functionName,
                 id);
      scope.Dispose();
    });
  }
}
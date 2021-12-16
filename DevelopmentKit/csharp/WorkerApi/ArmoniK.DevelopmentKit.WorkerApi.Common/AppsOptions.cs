/* IGridWorker.cs is part of the Htc.Mock solution.

   Copyright (c) 2021-2021 ANEO.
     D. DUBUC (https://github.com/ddubuc)

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.

*/
using Grpc.Core;

using System;
using System.Threading.Tasks;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Configuration;


namespace ArmoniK.DevelopmentKit.WorkerApi.Common
{
  public static class AppsOptions
  {
      public static string GridAppNameKey { get; } = "GridAppName";
      public static string GridAppVersionKey { get; } = "GridAppVersion";
      public static string GridAppNamespaceKey { get; } = "GridAppNamespace";
      public static string GridVolumesKey { get; } = "gridVolumes";
      public static string GridAppVolumesKey { get; } = "target_app_path";
  }
}

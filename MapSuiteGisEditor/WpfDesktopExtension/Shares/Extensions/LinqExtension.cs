/*
* Licensed to the Apache Software Foundation (ASF) under one
* or more contributor license agreements.  See the NOTICE file
* distributed with this work for additional information
* regarding copyright ownership.  The ASF licenses this file
* to you under the Apache License, Version 2.0 (the
* "License"); you may not use this file except in compliance
* with the License.  You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/


using System.Collections.Generic;

namespace System.Linq
{
    public static class LinqExtension
    {
        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    action?.Invoke(item);
                }
            }
        }

        public static T Dequeue<T>(this Queue<object> queue)
        {
            return (T)queue.Dequeue();
        }

        public static IEnumerable<T> Except<T>(this IEnumerable<T> items, T itemToExcept)
        {
            return items.Except(new T[] { itemToExcept });
        }
    }
}
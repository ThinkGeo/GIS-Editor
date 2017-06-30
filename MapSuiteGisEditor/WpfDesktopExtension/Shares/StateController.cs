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


using System;
using System.Collections.Generic;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    [Serializable]
    public class StateController<T>
    {
        [NonSerialized]
        private Stack<T> rollBackStack;

        [NonSerialized]
        private Stack<T> forwardStack;

        [NonSerialized]
        private bool firstForward = false;

        public StateController()
        {
            rollBackStack = new Stack<T>();
            forwardStack = new Stack<T>();
        }

        public bool CanRollBack
        {
            get
            {
                return CanRollBackCore();
            }
        }

        public bool CanForward
        {
            get
            {
                return CanForwardCore();
            }
        }

        protected virtual bool CanRollBackCore()
        {
            return rollBackStack.Count > 1;
        }

        protected virtual bool CanForwardCore()
        {
            return forwardStack.Count > 0;
        }

        public T RollBack()
        {
            firstForward = false;
            return RollBackCore();
        }

        protected virtual T RollBackCore()
        {
            if (forwardStack.Count == 0)
            {
                forwardStack.Push(rollBackStack.Pop());
            }
            T tmpT = rollBackStack.Pop();
            forwardStack.Push(tmpT);
            return tmpT;
        }

        public T Forward()
        {
            return ForwardCore();
        }

        protected virtual T ForwardCore()
        {
            if (!firstForward)
            {
                T tmpT = forwardStack.Pop();
                rollBackStack.Push(tmpT);
                firstForward = true;
            }
            T tmp1 = forwardStack.Pop();
            rollBackStack.Push(tmp1);
            return tmp1;
        }

        public void Add(T t)
        {
            if (rollBackStack.Count == 1)
            {
                var tmp = rollBackStack.Pop();
                rollBackStack.Push(tmp);
                rollBackStack.Push(tmp);
            }
            AddCore(t);
        }

        protected virtual void AddCore(T t)
        {
            rollBackStack.Push(t);
        }

        public void Clear()
        {
            var array = rollBackStack.ToArray();
            ClearCore();
            if (array.Length > 0) rollBackStack.Push(array[array.Length - 1]);
        }

        protected virtual void ClearCore()
        {
            rollBackStack.Clear();
            forwardStack.Clear();
        }
    }
}
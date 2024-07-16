using System;
using System.Collections.Generic;
using System.Reflection;

namespace Jing
{
    /// <summary>
    /// 信号触发器
    /// </summary>
    public class SignalTrigger
    {
        /// <summary>
        /// 触发条件
        /// </summary>
        [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
        public class ConditionAttribute : Attribute
        {
            /// <summary>
            /// 信号名称
            /// </summary>
            public string SignalName { get; }

            public ConditionAttribute(string signalName)
            {
                SignalName = signalName;
            }
        }

        /// <summary>
        /// 信号切换的委托事件
        /// </summary>
        public delegate void SignalSwitchedEvent(string signalName, bool isOn);

        /// <summary>
        /// 信号触发检查方法
        /// </summary>
        private readonly Dictionary<string, HashSet<MethodInfo>> _triggerCheckMethodInfoDict = new Dictionary<string, HashSet<MethodInfo>>();

        /// <summary>
        /// 信号观察表
        /// </summary>
        private readonly Dictionary<string, HashSet<SignalSwitchedEvent>> _signalWatchDict = new Dictionary<string, HashSet<SignalSwitchedEvent>>();

        /// <summary>
        /// 缓存的信号值
        /// </summary>
        private readonly Dictionary<string, bool> _cachedSignalValue = new Dictionary<string, bool>();

        public SignalTrigger(Assembly assembly)
        {
            InitTriggerCheckMethods(assembly);
        }

        /// <summary>
        /// 初始化触发器关联的方法
        /// </summary>
        void InitTriggerCheckMethods(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                var methodInfos = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var mi in methodInfos)
                {
                    var attrs = mi.GetCustomAttributes<ConditionAttribute>();
                    foreach (var att in attrs)
                    {
                        AddSignalCheckMethod(att.SignalName, mi);
                    }
                }
            }
        }

        /// <summary>
        /// 添加信号检查方法
        /// </summary>
        /// <param name="signalName"></param>
        /// <param name="mi"></param>
        public void AddSignalCheckMethod(string signalName, MethodInfo mi)
        {
            if (!mi.IsStatic)
            {
                throw new Exception("仅支持静态方法!");
            }

            if (mi.ReturnType != typeof(bool))
            {
                throw new Exception("方法返回值必须为bool!");
            }

            if (mi.GetParameters().Length > 0)
            {
                throw new Exception("方法不能含参数!");
            }

            if (false == _triggerCheckMethodInfoDict.ContainsKey(signalName))
            {
                _triggerCheckMethodInfoDict.Add(signalName, new HashSet<MethodInfo>());
            }

            _triggerCheckMethodInfoDict[signalName].Add(mi);
        }

        /// <summary>
        /// 关注信号
        /// </summary>
        /// <param name="signalName"></param>
        /// <param name="onSignalSwitched"></param>
        public void Watch(string signalName, SignalSwitchedEvent onSignalSwitched)
        {
            if (false == _signalWatchDict.ContainsKey(signalName))
            {
                _signalWatchDict.Add(signalName, new HashSet<SignalSwitchedEvent>());
            }

            var eventSet = _signalWatchDict[signalName];
            eventSet.Add(onSignalSwitched);
        }

        /// <summary>
        /// 解除信号关注
        /// </summary>
        /// <param name="signalName"></param>
        /// <param name="onSignalSwitched"></param>
        public void Unwatch(string signalName, SignalSwitchedEvent onSignalSwitched)
        {
            if (false == _signalWatchDict.ContainsKey(signalName))
            {
                return;
            }

            var eventSet = _signalWatchDict[signalName];
            eventSet.Remove(onSignalSwitched);
        }

        /// <summary>
        /// 检查信号，被观察的信号，如果有改变，则会触发回调
        /// </summary>
        public void CheckSignals()
        {
            foreach (var kv in _signalWatchDict)
            {
                var signalName = kv.Key;

                bool signalValue = GetSignalValue(signalName);
                if (_cachedSignalValue.TryGetValue(signalName, out bool cachedValue) && cachedValue == signalValue)
                {
                    //检查时发现相对于缓存值没有改变，则不触发事件
                    continue;
                }

                _cachedSignalValue[signalName] = signalValue;

                var eventSet = kv.Value;
                foreach (var action in eventSet)
                {
                    action?.Invoke(signalName, signalValue);
                }
            }
        }

        /// <summary>
        /// 同步信号，所有观察的信号都被回调一次。
        /// </summary>
        public void SyncSignals()
        {
            foreach (var kv in _signalWatchDict)
            {
                var signalName = kv.Key;

                bool signalValue = GetSignalValue(signalName);

                _cachedSignalValue[signalName] = signalValue;

                var eventSet = kv.Value;
                foreach (var action in eventSet)
                {
                    action?.Invoke(signalName, signalValue);
                }
            }
        }

        /// <summary>
        /// 检查信号
        /// </summary>
        /// <param name="signalName"></param>
        /// <returns></returns>
        public bool GetSignalValue(string signalName)
        {
            if (_triggerCheckMethodInfoDict.TryGetValue(signalName, out var miSet))
            {
                foreach (var mi in miSet)
                {
                    bool value = (bool)mi.Invoke(null, null);
                    if (false == value)
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }
    }
}
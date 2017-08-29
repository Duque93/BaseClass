using System;

namespace BaseClass.Core
{
    public interface IHook
    {
        object[] beforeFunction(String methodName,params object[] parametros);
        object afterFunction(String methodName, object valueReturned);
    }

    public class Hook<T> : IHook
    {
        Func<String, object[], object[]> callBackBeforeFunction;
        Func<String, object, T> callBackAfterFunction;
        public Hook(Func<String, object[], object[]> beforeFunction, Func<String,object,T> afterFunction)
        {
            this.callBackBeforeFunction = beforeFunction;
            this.callBackAfterFunction = afterFunction;
        }

        public object afterFunction(string methodName, object valueReturned)
        {
            return callBackAfterFunction(methodName, valueReturned);
        }

        public object[] beforeFunction(string methodName, params object[] parametros)
        {
            return callBackBeforeFunction(methodName,parametros);
        }
    }
}

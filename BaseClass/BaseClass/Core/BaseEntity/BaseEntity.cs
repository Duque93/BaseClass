using BaseClass.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class BaseEntity
    {
        //Dependencia en getTypeBuilder()
        private static Dictionary<Type, TypeBuilder> builders = new Dictionary<Type, TypeBuilder>();
        //Dependencia en Init()
        private static TypeBuilder getTypeBuilder(Type tipo)
        {
            TypeBuilder typeBuilder = null;
            if (builders.TryGetValue(tipo, out typeBuilder)) return typeBuilder;

            ModuleBuilder modBuilder = asmBuilder.DefineDynamicModule(
                asmBuilder.GetName().Name,
                asmBuilder.GetName().Name + ".dll");

            typeBuilder = modBuilder.DefineType(
                "Runtime" + tipo.Name,
                TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class,
                tipo);
            builders.Add(tipo, typeBuilder);
            return typeBuilder;
        }

        //Dependencia en Init()
        private static Dictionary<Type, dynamic> CreatedRuntimeInstances = new Dictionary<Type, dynamic>();
        protected static T Init<T>(IHook hook = null) where T : BaseEntity
        {
            if (CreatedRuntimeInstances.ContainsKey(typeof(T))) return CreatedRuntimeInstances[typeof(T)];

            Type baseTipo = typeof(T);
            TypeBuilder builder = BaseEntity.getTypeBuilder(baseTipo);

            InjectMethods(builder, baseTipo);

            Type runtimeType = null;
            try { runtimeType = builder.CreateType(); }
            catch (Exception e)
            {
                throw e;
                // throw new Exception(String.Format("The class {0} is not visible because it's private/internal/protected.Set it to Public", baseTipo.Name ));
            }
#if DEBUG
            asmBuilder.Save("Manifiest.dll");
#endif
            T _Instance = (T)Activator.CreateInstance(runtimeType);
            CreatedRuntimeInstances.Add(typeof(T), _Instance);
            return _Instance;
        }
        //Dependencia en getTypeBuilder() & Init()
        private static AssemblyBuilder asmBuilder;
        static BaseEntity(){
            Console.WriteLine("Initializing BaseEntity...");
            Assembly asm = Assembly.GetExecutingAssembly();
            AssemblyName asmName = new AssemblyName("Runtime" + asm.GetName());
            asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                asmName,
                AssemblyBuilderAccess.RunAndSave);
            installedHooksInfo = typeof(BaseEntity).GetField("installedHook",BindingFlags.Instance | BindingFlags.NonPublic);
        }

        //Dependencia en InjectMethods()
        private static MethodAttributes calculateAttributes(MethodAttributes methodAttributes)
        {
            MethodAttributes atributos = MethodAttributes.Final | MethodAttributes.ReuseSlot;
            foreach (MethodAttributes atributo in Enum.GetValues(typeof(MethodAttributes)))
                if (methodAttributes.HasFlag(atributo) && atributo != MethodAttributes.Virtual)
                    atributos |= atributo;
            return atributos;
        }
        /// <summary>
        /// Itero sobre todos los metodos de la clase QUE SEAN VIRTUALES
        /// para sobreescribirlos e injectar el codigo
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="tipo"></param>
        // Dependencia en Init
        private static void InjectMethods(TypeBuilder builder, Type tipo)
        {
            IEnumerator<MethodInfo> metodos =
                tipo.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(item => item.IsVirtual)
                .GetEnumerator();
            while (metodos.MoveNext())
            {
                MethodInfo metodo = metodos.Current;
                MethodAttributes atributos =
                    (metodo.IsPublic ? MethodAttributes.Public : MethodAttributes.Private) | MethodAttributes.ReuseSlot | MethodAttributes.HideBySig | MethodAttributes.Virtual;// | MethodAttributes.Final;
                    
                MethodBuilder methodBuilder = builder.DefineMethod(
                      metodo.Name, //nombre del metodo
                      atributos, //Atributos del metodo + Final
                      metodo.ReturnType, //tipo de valor que retorna
                      metodo.GetParameters().Select(item => item.ParameterType).ToArray<Type>() //Parametros del metodo
                    );
                RuntimeHelpers.PrepareMethod(metodo.MethodHandle); //check if method works in JIT-compiled method

                

                generateILCode(methodBuilder.GetILGenerator(),tipo, metodo);
            }
        }
        /*
        private static void defineNeededParams(MethodBuilder builder,MethodInfo methodInfo)
        {
            ConstructorInfo info = typeof(System.ParamArrayAttribute).GetConstructor(new Type[0]);
            ParameterInfo[] paramsInfos = methodInfo.GetParameters();
            for (int i = 0; i < paramsInfos.Length; i++)
                if (paramsInfos[i].ParameterType.IsArray)
                    builder.DefineParameter(1, ParameterAttributes.None, "argumentos").SetCustomAttribute(info, new byte[] { 01, 00, 00, 00 });
        }*/

        //Dependencia en static BaseEntity() & generateILCode()
        private static FieldInfo installedHooksInfo;
        //Dependencia en InjectMethods()
        private static void generateILCode(ILGenerator ilgen,Type tipo, MethodInfo injectedMethod)
        {
            Type[] args = new List<Type>(injectedMethod.GetParameters().Select((item) => item.ParameterType)).ToArray();

            MethodInfo beforeFunction_Hook = tipo.GetMethod("beforeFunction", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo afterFunction_Hook = tipo.GetMethod("afterFunction", BindingFlags.NonPublic | BindingFlags.Instance);

            OpCode afterCallInjectedMethod = OpCodes.Nop;
            OpCode valueToLoadInAfterFunction = OpCodes.Ldc_I4_0;
            OpCode valueToStoreAfterCallAfterFunction = OpCodes.Nop;
            OpCode valueToReturnOnTheEnd = OpCodes.Nop;

            LocalBuilder parametros = ilgen.DeclareLocal(typeof(object[]));//Stloc_0

            Type returnType = typeof(int);
            if (typeof(void) != injectedMethod.ReturnType)
            {
                returnType = injectedMethod.ReturnType;
                
                LocalBuilder valueToReturn = ilgen.DeclareLocal(returnType);//Stloc_1
                LocalBuilder returnedValue = ilgen.DeclareLocal(returnType);//Stloc_2

                afterCallInjectedMethod = OpCodes.Stloc_1;
                valueToLoadInAfterFunction = OpCodes.Ldloc_1;
                valueToStoreAfterCallAfterFunction = OpCodes.Stloc_2;
                valueToReturnOnTheEnd = OpCodes.Ldloc_2;
            }
            //////////////////////////////////////////////////////////////////////
            {
                //object parametros[] = base.beforeFunction("NombreMetodo", argumento1,argumento2,...);
                ilgen.Emit(OpCodes.Ldarg_0);//▼ base.
                ilgen.Emit(OpCodes.Ldstr, injectedMethod.Name);//"NombreMetodo"*1
                generateArray(ilgen //argumento1,argumento2,... -> *2
                    , typeof(object)
                    , args);
                ilgen.Emit(OpCodes.Call, beforeFunction_Hook);//3* = beforeFunction(*1,*2);
                ilgen.Emit(OpCodes.Stloc_0);//object params[] -> 3*
            }
            {
                updateArgs(ilgen,args);//args = params;
                //var returnValue = base.[injectedMethod]();
                ilgen.Emit(OpCodes.Ldarg_0);//▼ base.
                for (int i = 0; i < injectedMethod.GetParameters().Length; i++) ilgen.Emit(OpCodes.Ldarg, i + 1);
                ilgen.Emit(OpCodes.Call, injectedMethod); //▼ *2 = injectedMethod
                ilgen.Emit(afterCallInjectedMethod);// var returnValue; ->*2 || Nop
            }
            {
                ilgen.Emit(OpCodes.Ldarg_0);//▼ base.
                ilgen.Emit(OpCodes.Ldstr, injectedMethod.Name);//"NombreMetodo"-> *1
                ilgen.Emit(valueToLoadInAfterFunction);//returnValue -> *2 || 0
                ilgen.Emit(OpCodes.Box, returnType);
                ilgen.Emit(OpCodes.Call, afterFunction_Hook);//afterFunction(*1,*2);
                if (typeof(void) == injectedMethod.ReturnType) ilgen.Emit(OpCodes.Pop);
                else ilgen.Emit(OpCodes.Unbox_Any, returnType);// --> Cambiar por OpCodes.Pop
                ilgen.Emit(valueToStoreAfterCallAfterFunction); // var valueToReturn;
            }
            ilgen.Emit(valueToReturnOnTheEnd);
            ilgen.Emit(OpCodes.Ret); //return ;

        }

        private static void generateArray(ILGenerator emisor,Type tipoArray, params Type[] parametros)
        {
            if (parametros.Length == 0)
            {
                emisor.Emit(OpCodes.Ldnull);
                return;
            }
            emisor.Emit(OpCodes.Ldc_I4, parametros.Length);
            emisor.Emit(OpCodes.Newarr, tipoArray);
           
            for (int i = 0; i < parametros.Length; i++)
            {
                emisor.Emit(OpCodes.Dup);
                emisor.Emit(OpCodes.Ldc_I4, i); //En el indice i del array...
                emisor.Emit(OpCodes.Ldarg, i + 1);//introducimos el argumento i+1...
                if(parametros[i].IsPrimitive)
                    emisor.Emit(OpCodes.Box, parametros[i]); //si no es subclase, convertimos
                emisor.Emit(OpCodes.Stelem_Ref);// Guardamos el valor arg i+1 en el indice i del array
            }
               
        }
        private static void updateArgs(ILGenerator emisor, params Type[] parametros)
        {
            for(int i = 0; i < parametros.Length; i++)
            {
                emisor.Emit(OpCodes.Ldloc_0); //load array
                emisor.Emit(OpCodes.Ldc_I4, i); //index i
                emisor.Emit(OpCodes.Ldelem_Ref); // Cargamos array[i]
                if (parametros[i].IsPrimitive) emisor.Emit(OpCodes.Unbox_Any, parametros[i]);
                else emisor.Emit(OpCodes.Castclass, parametros[i]);
                emisor.Emit(OpCodes.Starg_S, i + 1);
            }
        }
        

        /////////////////////////////////////////////////////////////////////////////////////
        protected IDictionary<String, IHook> installedHook = new Dictionary<String, IHook>();
        public void installHook(string methodName, IHook hook )
        {
            if (this.GetType().GetMethod(methodName) == null) throw new Exception(String.Format("The method with name {0} can't be founded. Make sure it's virtual and it exists", methodName));
            installedHook.Add(methodName, hook);
        }
        //HOOK Implementation
        protected object[] beforeFunction(string methodName,params object[] parametros)
        {
            if (installedHook.ContainsKey(methodName)) return installedHook[methodName].beforeFunction(methodName, parametros);
            else return parametros;
        }

        protected object afterFunction(string methodName, object value)
        {
            if (installedHook.ContainsKey(methodName)) return installedHook[methodName].afterFunction(methodName, value);
            else return value;
        }
        //END HOOK Implementation
        /////////////////////////////////////////////////////////////////////////////////////

        
        protected IDictionary<String, ExtFieldInfo> fieldData = new Dictionary<String, ExtFieldInfo>();
    }

}

using BaseClass.Core;
using System;
using System.Reflection;

namespace Entities
{
    public class Test : BaseEntity
    {
        private static Test _entidad;
        public static Test Init()
        {
            if(_entidad == null) _entidad = BaseEntity.Init<Test>();
            
            _entidad.installHook(
                "getNumero",
                new Hook<int>(
                    new Func<string,object[],object[]>( (string methodName,object[] parametros) => { return parametros; }),
                    new Func<string, object, int>( (string methodName,object valueToReturn) => { return (int)valueToReturn; })
                   )
            );
            
            
            _entidad.installHook(
                "prueba",
                new Hook<int>(
                    new Func<string, object[],object[]>((string methodName, object[] parametros) => {
                        parametros[0] = 21312;
                        parametros[1] = "otra cosa";
                        return parametros;
                    }),
                    new Func<string, object, int>((string methodName, object valueToReturn) => { return 0; })
                   )
            );

            _entidad.installHook(
               "estoyTesteando",
               new Hook<int>(
                   new Func<string, object[], object[]>((string methodName, object[] parametros) => {
                       String[] cadenas = (String[])parametros[0];
                       cadenas[0] = "lo he cambiado";
                       cadenas[1] = "esto tambien";
                       return parametros;
                   }),
                   new Func<string, object, int>((string methodName, object valueToReturn) => { return 0; })
                  )
           );

            return _entidad;
        }

        int numero = 5645;
        public virtual int getNumero()
        {
            return numero;
        }
        
        public virtual void prueba(int a, string b)
        {
            prueba2(a, b);
        }
        
        
        public virtual void estoyTesteando(params string[] cadenas)
        {
            foreach (String cadena in cadenas)
                Console.WriteLine(cadena);
        }

        public void mirando(int a, string b)
        {
            object[] args = base.beforeFunction("testeo2",a,b);
            a = (int)args[0];
            b = (string)args[1];

            prueba2(a, b);

            base.afterFunction("testeo2", 0);
        }



        private void prueba2(int a , string b = "ook")
        {
            Console.WriteLine("He recibido el valor {0} y el string {1}", a, b);
        }


    }

    
}

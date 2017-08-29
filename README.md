# BaseClass
Clase Simple que permite instalar eventos onBeforeFunction y onAfterFunction para clases que extiendan de ella  
* OnBeforeFunction permite manipular los argumentos del metodo
* OnAfterFunction permite modificar el valor retornado (si retorna algo)

El funcionamiento se intenta mantener simple e intuitivo:
* Todo metodo que vaya a ser sobreescrito debe ser virtual
* Para instalar lo que sería el Hook, suponiendo una clase B que extiende a BaseClass y tiene un metodo 'public int metodo(int a, int b, string c)'
```c#
    _entidad.installHook(
               "metodo",
               new Hook<int/*tipo de valor que retorna*/>(
                   new Func<string, object[], object[]>((string methodName, object[] parametros) => {
                       int a = parametros[0];
                       int b = parametros[1];
                       int c = parametros[2];
                       Console.WriteLine("Recibido llamada a {0} onBeforeFunction", methodName);
                       c = "otra cosa"; //Cambiamos el valor del parametro.
                       return parametros;
                   }), //Función que será llamada onBeforeFunction
                   new Func<string, object, int>((string methodName, object valueToReturn) => { 
                      Console.WriteLine("Recibido llamada a {0} onAfterFunction", methodName);
                      return -5; //Modificamos el valor a retornar 
                      })) ///Función que será llamada onAfterFunction
           );
           //Lo unico que puede cambiar a gusto de cada uno es el cuerpo de la función.
```

using Entities;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BaseClass
{
    class Program
    {
        static void Main(string[] args)
        {

            /*Console.WriteLine();
            byte[] bodyIL = typeof(Program).GetMethod("prueba").GetMethodBody().GetILAsByteArray();
            foreach (byte @byte in bodyIL)
                Console.WriteLine(@byte.ToString("X"));

            MethodInfo info = typeof(Dictionary<string, int>).GetMethod("get_Item");*/

            var p = Test.Init();
            Console.WriteLine(p.getNumero());
            p.prueba(5,"vale");
            p.estoyTesteando("siii", "noooo", "valeeee","ooook");
            Console.ReadLine();
        }
        
    }
}

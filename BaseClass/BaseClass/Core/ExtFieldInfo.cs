using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BaseClass.Core
{
    public class ExtFieldInfo
    {
        public ExtFieldInfo(FieldInfo fieldInfo)
        {
            FieldInfo = fieldInfo;
        }

        public FieldInfo FieldInfo { get; private set; }

    }
}

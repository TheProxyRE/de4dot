using dnlib.DotNet;
using System.Collections.Generic;
using dnlib.DotNet.Emit;

namespace de4dot.code.deobfuscators.Manco_NET
{
    public class AntiTamper
    {
        List< MethodDef> tamperMethods = new List<MethodDef>();
        TypeDef tamperType;
        ModuleDef module;

        public bool Detected
        {
            get { return tamperType != null; }
        }
        public List<MethodDef> Methods
        {
            get { return tamperMethods; }
        }
        public TypeDef Type
        {
            get { return tamperType; }

        }
        public AntiTamper(ModuleDef Module)
        {
            module = Module;
        }
        public void Find()
        {
            foreach (var type in module.Types)
            {
                if (!type.IsAbstract && !type.IsSealed)
                    continue;
                foreach (var method in type.Methods)
                {
                    if (method.IsConstructor && !method.HasBody)
                        continue;
                    if (!IsTamperMethod(method))
                        continue;
                    tamperType = type;

                    foreach (var mtd in type.Methods)
                        if(mtd != method)
                        tamperMethods.Add(mtd);

                    return;
                }
            }
        }

        private bool IsTamperMethod(MethodDef method)
        {
            try
            {
                var instr = method.Body.Instructions;
                if (instr.Count != 2)
                    return false;
                
                if (instr[0].OpCode != OpCodes.Newobj)
                    return false;
                if (instr[0].Operand == null)
                    return false;
                if (!(instr[0].Operand is MemberRef))
                    return false;
                var mref = instr[0].Operand as MemberRef;

                if (mref.FullName != "System.Void System.StackOverflowException::.ctor()")
                    return false;
                if (instr[1].OpCode != OpCodes.Throw)
                    return false;
            }catch
            {
                return false;
            }
            return true;
        }
    }
}

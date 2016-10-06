using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Collections.Generic;
using static de4dot.code.deobfuscators.Manco_NET.StringDecryptor;

namespace de4dot.code.deobfuscators.Manco_NET
{
    class Junk
    {
        ModuleDef module;
        MethodDef junkMethod;
        FieldDef junkBool1;
        FieldDef junkBool2;

        public TypeDef Type
        {
            get { return junkMethod.DeclaringType; }
        }
        public Junk(ModuleDef Module)
        {
            module = Module;
        }
        public bool Detected
        {
            get { return junkBool1 != null && junkBool2 != null && junkMethod != null; }
        }

        public List<MethodDef> AdditionalMethods = new List<MethodDef>();

        public List<StringProxyField> AdditionalFields = new List<StringProxyField>();

        public void Find()
        {
            foreach (var type in module.Types)
            {
                if (!type.IsSealed)
                    continue;
                if (!type.IsAbstract)
                    continue;
                if (type.Methods.Count != 2)
                    continue;
                List<FieldDef> junkFields = new List<FieldDef>();
                foreach (var field in type.Fields)
                    if (field.IsStatic && field.FieldSig.ToString() == "System.Boolean")
                        junkFields.Add(field);
                if (junkFields.Count != 2)
                    continue;
                junkBool1 = junkFields[0];
                junkBool2 = junkFields[1];
                foreach (var method in type.Methods)
                    if (!method.IsConstructor && method.Body.Instructions.Count == 1
                        && method.Body.Instructions[0].OpCode == OpCodes.Ret)
                    {
                        junkMethod = method;
                        return;
                    }
            }
        }

        public void Fix()
        {
            foreach (var type in module.GetTypes())
                foreach (var method in type.Methods)
                {
                    if (!method.HasBody)
                        continue;
                    var instr = method.Body.Instructions;
                    for (int i = 0; i < instr.Count; i++)
                        if (instr[i].IsConditionalBranch())
                        {
                            if (instr[i - 1].OpCode != OpCodes.Ldsfld)
                                continue;
                            var field = instr[i - 1].Operand as FieldDef;
                            if (!(field == junkBool1 || field == junkBool2))
                                continue;
                            if (instr[i + 1].OpCode != OpCodes.Call)
                                continue;
                            if (instr[i + 1].Operand != junkMethod)
                                continue;

                            instr[i - 1].OpCode = OpCodes.Nop;
                            instr[i].OpCode = OpCodes.Nop;
                            instr[i + 1].OpCode = OpCodes.Nop;
                        }

                        else if (instr[i].OpCode == OpCodes.Ldsfld)
                        {
                            foreach (var field in AdditionalFields)
                                if (field.Field == instr[i].Operand)
                                {
                                    instr[i].OpCode = OpCodes.Ldstr;
                                    instr[i].Operand = field.Value;
                                    break;
                                }
                        }
                        else if (instr[i].OpCode == OpCodes.Call)
                            foreach (var mtd in AdditionalMethods)
                                if (instr[i].Operand == mtd)
                                {
                                    instr[i].OpCode = OpCodes.Nop;
                                    break;
                                }
                }
        }
    }
}

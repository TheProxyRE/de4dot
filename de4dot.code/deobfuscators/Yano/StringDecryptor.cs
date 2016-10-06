/*
    Copyright (C) 2016 TheProxy

    This file is part of modified de4dot.

    de4dot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    de4dot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with de4dot.  If not, see <http://www.gnu.org/licenses/>.
*/

using de4dot.blocks;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Collections.Generic;

namespace de4dot.code.deobfuscators.Yano
{
    class StringDecryptor
    {
        public StringDecryptor(ModuleDefMD Module)
        {
            module = Module;
        }
        ModuleDefMD module;
        List<OpCode> StringDecry = new List<OpCode> {
        OpCodes.Ldc_I4 , OpCodes.Ldarg_1 ,OpCodes.Add , OpCodes.Stloc_0,OpCodes.Ldarg_0,OpCodes.Call ,OpCodes.Stloc_1 , OpCodes.Ldc_I4_0 , OpCodes.Stloc_2 , OpCodes.Ldloc_2 ,OpCodes .Ldloc_1 ,OpCodes .Ldlen , OpCodes .Conv_I4 ,OpCodes.Clt };
        MethodDef strDecryptMethod;
        int key;

        public bool Detected
        {
            get
            {                return strDecryptMethod != null && key != null;            }
        }
        public MethodDef Method
        {
            get
            {                return strDecryptMethod;            }
        }

        public void Find()
        {

            foreach (var type in module.Types)
                foreach (var method in type.Methods)
                {
                    if (!method.HasBody)
                        continue;
                    if (method.ReturnType.ToString() != "System.String")
                        continue;
                    if (method.Parameters == null)
                        continue;
                    if (method.Parameters.Count != 2)
                        continue;
                    try
                    {
                        strDecryptMethod = DotNetUtils.FindMethod(module, StringDecry);
                        key = strDecryptMethod.Body.Instructions[0].GetLdcI4Value();
                    }
                    catch
                    { }
                }
        }

        public string Decrypt(string text, int num)
        {
            int k = key + num;
            char[] chArray = text.ToCharArray();
            int index = 0;
            while (true)
            {
                if (index >= chArray.Length)
                {
                    break;
                }
                char ch = chArray[index];
                chArray[index] = (char)((((ch & 0xff) ^ k++) << 8) | ((byte)((ch >> 8) ^ k++)));
                index++;
            }
            return string.Intern(new string(chArray));
        }

        public void DecryptMethod(MethodDef method)
        {
            var instr = method.Body.Instructions;

            for (int i = 0; i < instr.Count; i++)
            {

                if (instr[i].OpCode == OpCodes.Ldstr && instr[i + 1].IsLdcI4() && instr[i + 2].OpCode == OpCodes.Call && instr[i + 2].Operand == strDecryptMethod)
                {
                    instr[i].Operand = Decrypt(instr[i].Operand.ToString(), instr[i + 1].GetLdcI4Value());
                    instr[i + 1].OpCode = OpCodes.Nop;
                    instr[i + 2].OpCode = OpCodes.Nop;
                }
            }
        }
    }
}

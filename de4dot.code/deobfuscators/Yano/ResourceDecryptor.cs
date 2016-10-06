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
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

namespace de4dot.code.deobfuscators.Yano
{
    class ResourceDecryptor
    {
        public ResourceDecryptor(ModuleDefMD Module, StringDecryptor strDec)
        {
            module = Module;
            strDecryptor = strDec;
        }
        ModuleDefMD module;
        StringDecryptor strDecryptor;
        string resouceName;
        long key1;
        long key2;
        List<MethodDef> toremove = new List<MethodDef>();
        Instruction toremoveinstr;
        public List<MethodDef> toRemove
        {
            get { return toremove; }
        }

        public Instruction toRemoveCall
        {
            get { return toremoveinstr; }
        }
        public bool Detected
        {
            get
            { return !string.IsNullOrEmpty(resouceName) && key1 != null && key2 != null; }
        }
        public string Name
        {
            get
            { return resouceName; }
        }

        public void Find()
        {
            MethodDef moduleCtor = DotNetUtils.GetModuleTypeCctor(module);
            if (moduleCtor == null)
                return;
            var instr = moduleCtor.Body.Instructions;
            foreach (var inst in instr)
                if (inst.OpCode == OpCodes.Call && inst.Operand is MethodDef)
                {

                    MethodDef method = inst.Operand as MethodDef;

                    var instr2 = method.Body.Instructions;
                    if (instr2[0].OpCode != OpCodes.Call)
                        continue;
                    if (instr2[1].OpCode != OpCodes.Ldnull)
                        continue;
                    if (instr2[2].OpCode != OpCodes.Ldftn)
                        continue;
                    if (instr2[3].OpCode != OpCodes.Newobj)
                        continue;
                    if (instr2[4].OpCode != OpCodes.Callvirt)
                        continue;
                    MethodDef method2 = instr2[2].Operand as MethodDef;
                    if (strDecryptor.Detected) strDecryptor.DecryptMethod(method2);
                    var instr3 = method2.Body.Instructions;

                    resouceName = DotNetUtils.FindInstruction(instr3, OpCodes.Ldstr, 0).Operand.ToString();
                    key1 = (long)DotNetUtils.FindInstruction(instr3, OpCodes.Ldc_I8, 0).Operand;
                    key2 = (long)DotNetUtils.FindInstruction(instr3, OpCodes.Ldc_I8, 1).Operand;

                    toremoveinstr = inst;
                    toremove.Add(method);
                    toremove.Add(method2);
                }
        }


        public void FixResources()
        {
            byte[] decrypted = DecryptRes();
            if (decrypted != null)
            {
                AssemblyDef asm = AssemblyDef.Load(decrypted);
                foreach (var resource in asm.ManifestModule.Resources)
                    module.Resources.Add(resource as EmbeddedResource);
            }
        }

        byte[] DecryptRes()
        {
            int num;
            var res = DotNetUtils.GetResource(module, resouceName) as EmbeddedResource;
            Stream manifestResourceStream = res.GetResourceStream();
            if (manifestResourceStream != null)
            {
                Stream stream3 = new DeflateStream(new CryptoStream(manifestResourceStream, new DESCryptoServiceProvider().CreateDecryptor(BitConverter.GetBytes((ulong)key1), BitConverter.GetBytes((ulong)key2)), CryptoStreamMode.Read), CompressionMode.Decompress);
                MemoryStream stream2 = new MemoryStream();
                byte[] buffer = new byte[0x1000];
                while ((num = stream3.Read(buffer, 0, 0x1000)) != 0)
                    stream2.Write(buffer, 0, num);
                return stream2.ToArray();
            }
            return null;
        }
    }
}

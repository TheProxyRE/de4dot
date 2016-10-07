using System;
using System.Collections.Generic;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.IO;
using de4dot.blocks;
namespace de4dot.code.deobfuscators.NetPack
{
    class ResourceDecrypter
    {
        ModuleDefMD module; MethodDef resourceDecryptor;
        byte[] decryptedBytes;
        public bool Detected
        {
            get { return resourceDecryptor != null; }
        }
        public byte[] Decrypted
        {
            get { return decryptedBytes; }
        }
        public void Find()
        {
            MethodDef entrypoint = module.EntryPoint;
            if (entrypoint == null && !entrypoint.HasBody)
                return; var instr = entrypoint.Body.Instructions;
            for (int i = 0; i < instr.Count - 1; i++)
            {
                if (instr[i].OpCode != OpCodes.Ldc_I4_0)
                    continue;
                if (instr[i + 1].OpCode != OpCodes.Newarr)
                    continue;
                if (instr[i + 2].OpCode != OpCodes.Stloc_0)
                    continue;
                if (instr[i + 3].OpCode != OpCodes.Call)
                    continue;
                if (instr[i + 4].OpCode != OpCodes.Ldstr)
                    continue;
                if (instr[i + 5].OpCode != OpCodes.Callvirt)
                    continue;
                if (instr[i + 6].OpCode != OpCodes.Stloc_1)
                    continue;
                if (instr[i + 7].OpCode != OpCodes.Ldloc_1)
                    continue;
                if (instr[i + 8].OpCode != OpCodes.Callvirt)
                    continue;
                if (instr[i + 9].OpCode != OpCodes.Conv_Ovf_I)
                    continue;
                if (instr[i + 10].OpCode != OpCodes.Newarr)
                    continue;
                resourceDecryptor = entrypoint;
                decryptedBytes = Decrypt(instr[i + 4].Operand.ToString());
            }
        }
        byte[] Decrypt(string resName)
        {
            byte[] array = new byte[0];
            var resource = DotNetUtils.GetResource(module, resName) as EmbeddedResource;
            using (Stream manifestResourceStream = resource.GetResourceStream())
            {
                array = new byte[manifestResourceStream.Length];
                manifestResourceStream.Read(array, 0, array.Length);
                array = QuickLZ.decompress(array);
                return array;
            }
        }
        public ResourceDecrypter(ModuleDefMD module)
        {
            this.module = module;
        }
    }
}

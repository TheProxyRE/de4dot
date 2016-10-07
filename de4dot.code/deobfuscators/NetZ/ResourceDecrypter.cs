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

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using de4dot.blocks;
using System;
using System.Collections.Generic;
using System.Resources;
using System.IO;
using System.Reflection;
using System.Globalization;
using System.Collections;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace de4dot.code.deobfuscators.NetZ
{
    class ResourceDecrypter
    {
        ModuleDefMD module; MethodDef resourceDecryptor;
        IDeobfuscatedFile deobfuscator;
        byte[] decryptedBytes;

        Assembly assembly;

        List<DictionaryEntry> libraries = new List<DictionaryEntry>();

        public bool Detected
        {
            get { return resourceDecryptor != null; }
        }
        public byte[] Decrypted
        {
            get { return decryptedBytes; }
        }

        public ResourceDecrypter(ModuleDefMD module, IDeobfuscatedFile Deobfsucator)
        {
            this.module = module;
            deobfuscator = Deobfsucator;
        }

        public void Find()
        {
            MethodDef entrypoint = module.EntryPoint;
            if (entrypoint == null && !entrypoint.HasBody)
                return;
            var instr = entrypoint.Body.Instructions;

            for (int i = 0; i < instr.Count - 1; i++)
                if (instr[i].OpCode == OpCodes.Call)
                {
                    if (instr[i].Operand == null)
                        continue;

                    var mtd = instr[i].Operand as MethodDef;

                    if (mtd == null)
                        continue;

                    if (mtd.Signature.ToString() != "System.Int32 (System.String[])")
                        continue;

                    var mtdInstr = mtd.Body.Instructions;
                    int callIndex;
                    var inst = DotNetUtils.FindInstruction(mtdInstr, OpCodes.Ldstr, 0, out callIndex);

                    if (inst == null)
                        continue;

                    if (mtdInstr[callIndex + 1].OpCode != OpCodes.Call)
                        continue;

                    var mtdGetRes = mtdInstr[callIndex + 1].Operand as MethodDef;

                    if (mtdGetRes == null)
                        continue;

                    if (mtdGetRes.Signature.ToString() != "System.Byte[] (System.String)")
                        continue;

                    var inst2 = DotNetUtils.FindInstruction(mtdGetRes.Body.Instructions, OpCodes.Ldstr, 0);

                    if (inst2 == null)
                        continue;

                    var resource = DotNetUtils.GetResource(module, inst2.Operand.ToString() + ".resources");

                    if (resource == null)
                        continue;

                    if (assembly == null) assembly = DotNetUtils.ModuleDefToAssembly(module);

                    if (assembly == null)
                        continue;

                    ResourceManager resManager = new ResourceManager(inst2.Operand.ToString(), assembly);

                    var resourceSet = resManager.GetResourceSet(CultureInfo.CurrentUICulture, true, true);

                    foreach (DictionaryEntry res in resourceSet)
                    {
                        if (res.Key.ToString() == inst.Operand.ToString())
                            decryptedBytes = UnZip((byte[])res.Value);
                        else if (res.Key.ToString() != "zip.dll")
                            libraries.Add(res);
                    }
                    return;
                }
        }


        public void ExtractLibraries()
        {
            if (libraries.Count == 0)
                return;

            foreach (var res in libraries)
            {
                string realName = res.Key.ToString().Split(new string[] { "!2!1" }, StringSplitOptions.None)[0];
                byte[] decrypted = UnZip((byte[])res.Value);
                deobfuscator.CreateAssemblyFile(decrypted, realName, null);
            }
        }

      


        private static byte[] UnZip(byte[] data)
        {
            if (data == null)
            {
                return null;
            }
            MemoryStream baseInputStream = null;
            MemoryStream stream2;
            InflaterInputStream stream3 = null;
            try
            {
                baseInputStream = new MemoryStream(data);
                stream2 = new MemoryStream();
                stream3 = new InflaterInputStream(baseInputStream);
                byte[] buffer = new byte[data.Length];
                while (true)
                {
                    int count = stream3.Read(buffer, 0, buffer.Length);
                    if (count <= 0)
                    {
                        break;
                    }
                    stream2.Write(buffer, 0, count);
                }
                stream2.Flush();
                stream2.Seek(0L, SeekOrigin.Begin);
            }
            finally
            {
                if (baseInputStream != null)
                {
                    baseInputStream.Close();
                }
                if (stream3 != null)
                {
                    stream3.Close();
                }
            }
            stream2.Seek(0L, SeekOrigin.Begin);
            return stream2.ToArray();
        }
    }
}

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace de4dot.code.deobfuscators.Manco_NET
{
    class StringDecryptor
    {
        ModuleDef module;
        MethodDef scrambleMethod,decryptMethod;
        ISimpleDeobfuscator deobfsucator;


        List<StringProxyField> proxfelds = new List<StringProxyField>();
        public List<StringProxyField> ProxyFields
        {
            get { return proxfelds; }
        }
        List<TypeDef> decTypes = new List<TypeDef>();

        public List<TypeDef> Types
        {
            get { return decTypes; }
        }
        public bool Detected
        {
            get { return scrambleMethod != null || decryptMethod != null; }
        }
     
        public StringDecryptor(ModuleDef Module, ISimpleDeobfuscator Deobfsucator)
        {
            module = Module;
            deobfsucator = Deobfsucator;
        }

        class EncryptionTypes
        {
            public int Num;
            public string Key;
            public Encoding Enc;
            public MethodDef Method;
            public EncryptionTypes(MethodDef method, int number, string key, Encoding encoding)
            {
                Method = method;
                Num = number;
                Key = key;
                Enc = encoding;
            }
        }

         public class StringProxyField
        {
            public FieldDef Field { get; set; }
            public string Value { get; set; }
        }

        List<EncryptionTypes> decryptMethods = new List<EncryptionTypes>();
        public void Find(ISimpleDeobfuscator deobfsucator)
        {
            foreach (var type in module.Types)
            {
                if (!type.IsSealed)
                    continue;
                if (!type.IsAbstract)
                    continue;

                foreach (var method in type.Methods)
                {
                    deobfsucator.Deobfuscate(method);
                    var instr = method.Body.Instructions;

                    if (IsScrambleMethod(instr))
                    {
                        scrambleMethod = method;
                        decTypes.Add(type);

                    }
                    if (isDecryptionMethod(instr))
                    {
                        decryptMethod = method;
                        decTypes.Add(type);
                    }
                }
            }
        }


        public void Fix()
        {
            if (decryptMethod != null)
            {
                MethodDef typeCtor = null;
                foreach (var mtd in decryptMethod.DeclaringType.Methods)
                    if (mtd.IsConstructor)
                        typeCtor = mtd;
                    else if (mtd == decryptMethod)
                        continue;
                    else
                    {
                        deobfsucator.Deobfuscate(mtd);
                        FindEncryptinCall(mtd, decryptMethod);
                    }
                if (typeCtor != null && decryptMethods.Count != 0)
                {
                    var instr = typeCtor.Body.Instructions;
                    for (int i = 0; i < instr.Count; i++)
                    {
                        if (instr[i].OpCode == OpCodes.Ldstr && instr[i + 1].OpCode == OpCodes.Call && instr[i + 2].OpCode == OpCodes.Stsfld && instr[i + 2].Operand != null)
                            foreach (var decMethod in decryptMethods)
                                if (instr[i + 1].Operand == decMethod.Method)
                                {
                                    var field = instr[i + 2].Operand as FieldDef;
                                    string decrypted = Decrypt(decMethod.Num, instr[i].Operand.ToString(), decMethod.Key, decMethod.Enc);
                                    proxfelds.Add(new StringProxyField() { Field = field, Value = decrypted });
                                }
                    }
                }
            }

            if (scrambleMethod != null)
            {
                MethodDef typeCtor = null;
                foreach (var mtd in scrambleMethod.DeclaringType.Methods)
                    if (mtd.IsConstructor)
                        typeCtor = mtd;
                if (typeCtor == null)
                    return;
                var ctorInstr = typeCtor.Body.Instructions;
                for (int j = 0; j < ctorInstr.Count; j++)
                {
                    if (ctorInstr[j].OpCode == OpCodes.Ldstr && ctorInstr[j + 1].OpCode == OpCodes.Call && ctorInstr[j + 1].Operand == scrambleMethod && ctorInstr[j + 2].OpCode == OpCodes.Stsfld && ctorInstr[j + 2].Operand != null)
                    {
                        var field = ctorInstr[j + 2].Operand as FieldDef;
                        string unscrambled = Unscramble(ctorInstr[j].Operand.ToString());
                        proxfelds.Add(new StringProxyField() { Field = field, Value = unscrambled });
                    }
                }
            }
        }


    

        void FindEncryptinCall(MethodDef method, MethodDef call)
        {
            var instr = method.Body.Instructions;
            if (!instr[0].IsLdcI4())
                return;
            if (instr[2].OpCode != OpCodes.Ldstr)
                return;
            if (instr[3].OpCode != OpCodes.Call)
                return;
            if (instr[4].OpCode != OpCodes.Call)
                return;
            if (instr[4].Operand != call)
                return;
            decryptMethods.Add(new EncryptionTypes(method, instr[0].GetLdcI4Value(), instr[2].Operand.ToString(), GetEncoding(instr[3])));
        }


        Encoding GetEncoding(Instruction inst)
        {
            switch (inst.Operand.ToString())
            {
                case "System.Text.Encoding System.Text.Encoding::get_Default()":
                    return Encoding.Default;
                case "System.Text.Encoding System.Text.Encoding::get_Unicode()":
                    return Encoding.Unicode;
                case "System.Text.Encoding System.Text.Encoding::get_ASCII()":
                    return Encoding.ASCII;
                case "System.Text.Encoding System.Text.Encoding::get_UTF32()":
                    return Encoding.UTF32;
                case "System.Text.Encoding System.Text.Encoding::get_UTF8()":
                    return Encoding.UTF8;
                case "System.Text.Encoding System.Text.Encoding::get_UTF7()":
                    return Encoding.UTF7;
                case "System.Text.Encoding System.Text.Encoding::get_BigEndianUnicode()":
                    return Encoding.BigEndianUnicode;
            }
            return null;
        }

        bool isDecryptionMethod(IList<Instruction> instr)
        {
            for (int i = 0; i < instr.Count; i++)
            {
                if (!instr[i].IsLdarg())
                    continue;
                if (!instr[i + 1].IsLdarg())
                    continue;
                if (instr[i + 2].OpCode != OpCodes.Call)
                    continue;
                if (!instr[i + 3].IsStloc())
                    continue;
                if (!instr[i + 4].IsLdloc())
                    continue;
                if (instr[i + 5].OpCode != OpCodes.Ldnull)
                    continue;
                if (instr[i + 6].OpCode != OpCodes.Ceq)
                    continue;
                if (instr[i + 7].OpCode != OpCodes.Brtrue_S)
                    continue;
                if (!instr[i + 8].IsLdloc())
                    continue;
                if (instr[i + 9].OpCode != OpCodes.Callvirt)
                    continue;
                if (!instr[i + 10].IsStloc())
                    continue;
                if (!instr[i + 11].IsLdarg())
                    continue;
                if (instr[i + 12].OpCode != OpCodes.Call)
                    continue;
                if (instr[i + 12].Operand.ToString() != "System.Byte[] System.Convert::FromBase64String(System.String)")
                    continue;
                return true;
            }
            return false;
        }
        bool IsScrambleMethod(IList<Instruction> instr)
        {
            for (int i = 0; i < instr.Count; i++)
            {
                if (!instr[i].IsLdcI4())
                    continue;
                if (!instr[i + 1].IsStloc())
                    continue;
                if (!instr[i + 2].IsLdarg())
                    continue;
                if (instr[i + 3].OpCode != OpCodes.Ldsfld)
                    continue;
                if (instr[i + 4].OpCode != OpCodes.Call)
                    continue;
                if (!instr[i + 5].IsLdcI4())
                    continue;
                if (instr[i + 6].OpCode != OpCodes.Ceq)
                    continue;
                if (instr[i + 7].OpCode != OpCodes.Brtrue)
                    continue;
                if (!instr[i + 8].IsLdarg())
                    continue;
                if (instr[i + 9].OpCode != OpCodes.Callvirt)
                    continue;
                if (!instr[i + 10].IsLdcI4())
                    continue;
                if (instr[i + 11].OpCode != OpCodes.Div)
                    continue;
                return true;

            }
            return false;
        }


        #region DecryptionFunctions

        private static SymmetricAlgorithm smethod_0(int int_0)
        {
            SymmetricAlgorithm result;
            switch (int_0)
            {
                case 0:
                    result = new DESCryptoServiceProvider();
                    break;
                case 1:
                    result = new RC2CryptoServiceProvider();
                    break;
                case 2:
                    result = new RijndaelManaged();
                    break;
                case 3:
                    result = new TripleDESCryptoServiceProvider();
                    break;
                default:
                    throw new ArgumentOutOfRangeException("ServiceIndex");
            }
            return result;
        }

        private static SymmetricAlgorithm smethod_1(int int_0, string string_2014)
        {
            SymmetricAlgorithm symmetricAlgorithm = smethod_0(int_0);
            ASCIIEncoding aSCIIEncoding = new ASCIIEncoding();
            symmetricAlgorithm.IV = aSCIIEncoding.GetBytes(string_2014.Substring(0, 8));
            symmetricAlgorithm.Key = aSCIIEncoding.GetBytes(string_2014.Substring(8));
            return symmetricAlgorithm;
        }

        private static string Decrypt(int int_0, string string_2014, string string_2015, Encoding encoding_0)
        {
            SymmetricAlgorithm symmetricAlgorithm = smethod_1(int_0, string_2015);
            string result;
            if (symmetricAlgorithm != null)
            {
                ICryptoTransform cryptoTransform = symmetricAlgorithm.CreateDecryptor();
                byte[] array = Convert.FromBase64String(string_2014);
                byte[] array2 = cryptoTransform.TransformFinalBlock(array, 0, array.Length);
                result = encoding_0.GetString(array2, 0, array2.Length);
            }
            else
                result = "";
            return result;
        }

        internal string Unscramble(string scrambledString)
        {
            string result;
            if (scrambledString != string.Empty)
            {
                int num = scrambledString.Length / 2 + scrambledString.Length % 2;
                string text = scrambledString.Substring(0, num);
                string text2 = scrambledString.Substring(num);
                StringBuilder stringBuilder = new StringBuilder(scrambledString.Length);
                char[] array = new char[text.Length];
                char[] array2 = new char[text2.Length];
                bool flag = text.Length % 2 == 0;
                for (int i = 0; i < text.Length; i += 2)
                {
                    array[i / 2] = text[i];
                    if (i < text.Length - 1 || flag)
                        array[text.Length - i / 2 - 1] = text[i + 1];
                }
                flag = (text2.Length % 2 == 0);
                for (int i = 0; i < text2.Length; i += 2)
                {
                    array2[i / 2] = text2[i];
                    if (i < text2.Length - 1 || flag)
                        array2[text2.Length - i / 2 - 1] = text2[i + 1];
                }
                flag = (text.Length == text2.Length);
                for (int i = 0; i < text.Length; i++)
                {
                    stringBuilder.Append(array[i]);
                    if (i < text.Length - 1 || flag)
                        stringBuilder.Append(array2[i]);
                }
                result = stringBuilder.ToString();
            }
            else
                result = string.Empty;
            return result;

        }
        #endregion
    }
}

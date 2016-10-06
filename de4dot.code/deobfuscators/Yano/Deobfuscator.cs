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
using System;
using System.Collections.Generic;
namespace de4dot.code.deobfuscators.Yano
{
    public class DeobfuscatorInfo : DeobfuscatorInfoBase
    {
        public const string THE_NAME = "Yano";
        public const string THE_TYPE = "yn";
        const string DEFAULT_REGEX = @"!^[a-zA-Z1-9]{1,4}$" + DeobfuscatorBase.DEFAULT_VALID_NAME_REGEX;

        public DeobfuscatorInfo()
            : base(DEFAULT_REGEX)
        {
        }

        public override string Name
        {
            get { return THE_NAME; }
        }

        public override string Type
        {
            get { return THE_TYPE; }
        }

        public override IDeobfuscator CreateDeobfuscator()
        {
            return new Deobfuscator(new Deobfuscator.Options
            {
                RenameResourcesInCode = false,
                ValidNameRegex = validNameRegex.Get(),
            });
        }

        class Deobfuscator : DeobfuscatorBase
        {
            bool foundAttribute = false;
            string Version = "";
            TypeDef YanoAttribute;
            ResourceDecryptor resDecryptor;
            StringDecryptor strDecryptor;
            internal class Options : OptionsBase
            {
            }

            public override string Type
            {
                get { return DeobfuscatorInfo.THE_TYPE; }
            }

            public override string TypeLong
            {
                get { return DeobfuscatorInfo.THE_NAME; }
            }

            public override string Name
            {
                get { return string.Format("{0} {1}", TypeLong, Version); }
            }

            public Deobfuscator(Options options)
                : base(options)
            {
            }

            protected override int DetectInternal()
            {
                int val = 0;
                if (foundAttribute) val = 100;
                val += val < 100 ? Convert.ToInt32(resDecryptor.Detected) * 100 : Convert.ToInt32(resDecryptor.Detected) * 10;
                val += val < 100 ? Convert.ToInt32(strDecryptor.Detected) * 100 : Convert.ToInt32(strDecryptor.Detected) * 10;
                return val;
            }

            protected override void ScanForObfuscator()
            {
                foreach (var type in module.Types)
                    if (type.FullName == "YanoAttribute")
                    {
                        foundAttribute = true;
                        foreach (var field in type.Fields)
                            if (field.Name == "Version")
                                Version = (string)field.Constant.Value;
                        YanoAttribute = type;
                    }
                strDecryptor = new StringDecryptor(module);
                strDecryptor.Find();
                resDecryptor = new ResourceDecryptor(module, strDecryptor);
                resDecryptor.Find();

            }

            public override void DeobfuscateBegin()
            {
                base.DeobfuscateBegin();
                if (resDecryptor.Detected) resDecryptor.FixResources();
                if (strDecryptor.Detected)
                    staticStringInliner.Add(strDecryptor.Method, (method, gim, args) => strDecryptor.Decrypt((string)args[0], (int)args[1]));
            }

            public override void DeobfuscateEnd()
            {
                if (resDecryptor.toRemoveCall != null)
                {
                    DotNetUtils.GetModuleTypeCctor(module).Body.Instructions.Remove(resDecryptor.toRemoveCall);
                    module.Resources.Remove(DotNetUtils.GetResource(module, resDecryptor.Name));
                }
                foreach (var method in resDecryptor.toRemove)
                    method.DeclaringType.Methods.Remove(method);
                if (strDecryptor.Detected) module.Types.Remove(strDecryptor.Method.DeclaringType);

                AddTypeToBeRemoved(YanoAttribute, "Yano Attribute");
                base.DeobfuscateEnd();
            }

            public override IEnumerable<int> GetStringDecrypterMethods()
            {
                var list = new List<int>();

                return list;
            }

        }

    }
}
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
using System.Collections.Generic;

namespace de4dot.code.deobfuscators.NetZ
{
    public class DeobfuscatorInfo : DeobfuscatorInfoBase
    {
        public const string THE_NAME = "NetZ";
        public const string THE_TYPE = "nz";
        const string DEFAULT_REGEX = DeobfuscatorBase.DEFAULT_ASIAN_VALID_NAME_REGEX;
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
                ValidNameRegex = validNameRegex.Get(),
            });
        }
        protected override IEnumerable<Option> GetOptionsInternal()
        {
            return new List<Option>() { };
        }
    }
    class Deobfuscator : DeobfuscatorBase
    {
        Options options;
        bool netPackAttribute = false;
        ResourceDecrypter resDecryptor;
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
            get { return TypeLong; }
        }
        public Deobfuscator(Options options) : base(options)
        {
            this.options = options;
        }
        protected override int DetectInternal()
        {
            int val = 0;
            if (netPackAttribute)
                val += 10;
            if (resDecryptor.Detected)
                val += 100;
            return val;
        }
        void findNetPackAttribute()
        {
            foreach (var type in module.Types)
                if (type.Namespace == "netz")
                    netPackAttribute = true;
        }
        protected override void ScanForObfuscator()
        {
            findNetPackAttribute();
            resDecryptor = new ResourceDecrypter(module,DeobfuscatedFile);
            resDecryptor.Find();
        }
        public override bool GetDecryptedModule(int count, ref byte[] newFileData, ref DumpedMethods dumpedMethods)
        {
            if (count != 0)
                return false;
            newFileData = resDecryptor.Decrypted;
            resDecryptor.ExtractLibraries();
            return true;

        }
        public override IDeobfuscator ModuleReloaded(ModuleDefMD module)
        {
            Options newOptions = new Options();
            var newOne = new Deobfuscator(options);
            newOne.SetModule(module);
            return newOne;
        }
        public override void DeobfuscateBegin()
        {
            base.DeobfuscateBegin();
        }

        
        public override IEnumerable<int> GetStringDecrypterMethods()
        {
            return new List<int>();
        }
    }
}

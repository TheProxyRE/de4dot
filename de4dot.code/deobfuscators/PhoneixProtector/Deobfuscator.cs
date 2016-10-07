using System.Collections.Generic;
using dnlib.DotNet;
using de4dot.blocks;

namespace de4dot.code.deobfuscators.PhoneixProtector
{
    public class DeobfuscatorInfo : DeobfuscatorInfoBase
    {
        public const string THE_NAME = "Phoneix Protector";
        public const string THE_TYPE = "pp";
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
                RenameResourcesInCode = false,
                ValidNameRegex = validNameRegex.Get(),
            });
        }
    }

    class Deobfuscator : DeobfuscatorBase
    {
        Options options;
        string obfuscatorName = "Phoneix Protector";

        StringDecrypter stringDecrypter;
        bool foundPhoneixAttribute = false;

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
            get { return obfuscatorName; }
        }

        public Deobfuscator(Options options)
            : base(options)
        {
            this.options = options;
        }


        protected override int DetectInternal()
        {

            int val = 0;
            if (stringDecrypter.Detected)
                val += 100;
            if (foundPhoneixAttribute)
                val += 10;
            return val;
        }
        protected override void ScanForObfuscator()
        {
            stringDecrypter = new StringDecrypter(module);
            stringDecrypter.Find(DeobfuscatedFile);
            FindPhoneixAttribute();
        }

        void FindPhoneixAttribute()
        {
            foreach (var type in module.Types)
                if (type.Namespace.StartsWith("?") && type.Namespace.EndsWith("?"))
                {
                    foundPhoneixAttribute = true;
                    return;
                }
                else if (type.FullName.Contains("OrangeHeapAttribute"))
                {
                    foundPhoneixAttribute = true;
                    obfuscatorName = "Orange Heap";
                    AddAttributeToBeRemoved(type, "Atribute");
                    return;
                }
        }

        public override void DeobfuscateBegin()
        {
            base.DeobfuscateBegin();
            staticStringInliner.Add(stringDecrypter.Method, (method, gim, args) => stringDecrypter.Decrypt((string)args[0]));
            DeobfuscatedFile.StringDecryptersAdded();
        }

        public override void DeobfuscateEnd()
        {
            if (CanRemoveStringDecrypterType)
            {
                AddTypeToBeRemoved(stringDecrypter.Type, "String Derypter Type");
            }
            base.DeobfuscateEnd();
        }

        public override IEnumerable<int> GetStringDecrypterMethods()
        {
            var list = new List<int>();
            list.Add(stringDecrypter.Method.MDToken.ToInt32());
            return list;
        }
    }
}

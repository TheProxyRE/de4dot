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

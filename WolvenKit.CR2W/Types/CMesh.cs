using System.Collections.Generic;
using System.IO;
using WolvenKit.CR2W.Editors;

namespace WolvenKit.CR2W.Types
{
    class CMesh : CVector
    {
        public CByteArray padding;
        public CInt32 numberOfBones;
        public CArray jointStringIndices;
        public CInt32 numberOfBoneRigMatrices;
        public CArray boneRigMatrices;
        public CInt32 numberOfBoneVertexEpsilons;
        public CArray boneVertexEpsilons;
        public CInt32 numberOfFinalIndices;
        public CArray finalIndices;

        public CMesh(CR2WFile cr2w)
            : base(cr2w)
        {
            padding = new CByteArray(cr2w) { Name = "padding ?", Type = "CByteArray" };
            numberOfBones = new CInt32(cr2w) { Name = "numberOfBones", Type = "CInt32" };
            jointStringIndices = new CArray("array:0,ptr:CUint16", "CInt16", true, cr2w) { Name = "jointStringIndices", Type = "array:0,ptr:CInt16" };
            numberOfBoneRigMatrices = new CInt32(cr2w) { Name = "numberOfBoneRigMatrices", Type = "CInt32" };
            boneRigMatrices = new CArray("array:0,0,ptr:CFloat", "CFloat", true, cr2w) { Name = "boneRigMatrices", Type = "array:0,0,ptr:CFloat" };
            numberOfBoneVertexEpsilons = new CInt32(cr2w) { Name = "numberOfBoneVertexEpsilons ?", Type = "CInt32" };
            boneVertexEpsilons = new CArray("array:0,ptr:CFloat", "CFloat", true, cr2w) { Name = "boneVertexEpsilons ?", Type = "array:0,ptr:CFloat" };
            numberOfFinalIndices = new CInt32(cr2w) { Name = "numberOfFinalIndices ?", Type = "CInt32" };
            finalIndices = new CArray("array:0,ptr:CUint32", "CUint32", true, cr2w) { Name = "finalIndices ?", Type = "array:0,ptr:CUint32" };
        }

        public override void Read(BinaryReader file, uint size)
        {
            base.Read(file, size);

            var bytesArray = new List<byte>();
            var arrLength = file.ReadByte();
            bytesArray.Add(arrLength);
            CArray arr = new CArray("array:0,0,ptr:CInt16", "CInt16", false, cr2w) { Name = "padding ?", Type = "array:0,0,ptr:CInt16" };
            for (int i = 0; i < arrLength; i++)
            {
                var arrMembersLength = file.ReadByte();
                bytesArray.Add(arrMembersLength);
                CArray arrMember = new CArray("array:0,ptr:CInt16", "CInt16", false, cr2w) { Name = "padding ?", Type = "array:0,ptr:CInt16" };
                for (int j = 0; j < arrMembersLength; j++)
                {
                    CInt16 item = new CInt16(null) { Type = "CInt16" };
                    item.Read(file, 0);
                    arrMember.array.Add(item);
                    bytesArray.AddRange(System.BitConverter.GetBytes(item.val));
                }
                CFloat crc = new CFloat(null) { Type = "CFloat" };
                crc.Read(file, 0);
                arrMember.array.Add(crc);
                arr.array.Add(arrMember);
                bytesArray.AddRange(System.BitConverter.GetBytes(crc.val));
            }
            padding.Bytes = bytesArray.ToArray();

            numberOfBones.val = file.ReadBit6();
            for (int i = 0; i < numberOfBones.val; i++)
            {
                CInt16 stringIdx = new CInt16(null) { Type = "CInt16" };
                stringIdx.Read(file, 0);
                jointStringIndices.array.Add(stringIdx);
            }

            numberOfBoneRigMatrices.val = file.ReadBit6();
            for (uint i = 0; i < numberOfBoneRigMatrices.val; i++)
            {
                CArray matrix = new CArray("array:0,ptr:CFloat", "CFloat", true, cr2w) { Name = i.ToString(), Type = "array:0,ptr:CFloat" };
                for (int j = 0; j < 16; j++)
                {
                    CFloat value = new CFloat(null) { Type = "CFloat" };
                    value.Read(file, 0);
                    matrix.array.Add(value);
                }
                boneRigMatrices.array.Add(matrix);
            }

            numberOfBoneVertexEpsilons.val = file.ReadBit6();
            for (uint i = 0; i < numberOfBoneVertexEpsilons.val; i++)
            {
                CFloat value = new CFloat(null) { Type = "CFloat" };
                value.Read(file, 0);
                boneVertexEpsilons.array.Add(value);
            }

            numberOfFinalIndices.val = file.ReadBit6();
            for (uint i = 0; i < numberOfFinalIndices.val; i++)
            {
                CUInt32 value = new CUInt32(null) { Type = "CUInt32" };
                value.Read(file, 0);
                finalIndices.array.Add(value);
            }

            AddVariable(padding);
            AddVariable(numberOfBones);
            AddVariable(jointStringIndices);
            AddVariable(numberOfBoneRigMatrices);
            AddVariable(boneRigMatrices);
            AddVariable(numberOfBoneVertexEpsilons);
            AddVariable(boneVertexEpsilons);
            AddVariable(numberOfFinalIndices);
            AddVariable(finalIndices);
        }

        public override CVariable SetValue(object val)
        {
            return this;
        }

        public override CVariable Create(CR2WFile cr2w)
        {
            return new CMesh(cr2w);
        }
    }
}
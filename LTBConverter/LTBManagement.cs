using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace LTBConverter
{
    public class LTBManagement
    {

        public static int EncodingCodePage = Encoding.UTF8.CodePage;

        public static T ByteToType<T>(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(Marshal.SizeOf(typeof(T)));

            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();

            return theStructure;
        }

        public static void TypeToByte<T>(BinaryWriter writer, T data)
        {
            IntPtr pnt = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(T)));
            Marshal.StructureToPtr(data, pnt, false);
            
            byte[] managedArray = new byte[Marshal.SizeOf(typeof(T))];
            Marshal.Copy(pnt, managedArray, 0, Marshal.SizeOf(typeof(T)));
            Marshal.FreeHGlobal(pnt);
            
            writer.Write(managedArray);
        }

        public static void PadBytes(BinaryWriter writer, int padtovalue)
        {
            while (writer.BaseStream.Position % padtovalue != 0)
            {
                writer.Write((byte)0);
            }
        }

        public struct LtbStruct
        {
            public uint unk4_0000;
            public int uniqueentrycount;
            public Int64 textelemcount;
            public Int64 textaddressoffset;
            public Int64 desccount;
            public Int64 descaddressoffset;
            public Int64 languagecontainerscount;
            public Int64 languagecontainersoffset;
            public Int64 padding;
        }

        public struct StringCtr
        {
            public Int64 index;
            public Int64 type_always_0x10;
            public string value;
        }

        public struct DescArray
        {
            public Int64 itemcount;
            public Int64 type_always_0x10;
            public Int64[] stringpointers;
            public StringCtr[] strings;
        }

        public struct Entry
        {
            public string desc;
            public Int64 descindex;
            public Int64 desccontainerindex;
            public List<string> values;
        }

        public struct Language
        {
            public Int64 langcontainerindex;
            public Int64 langindex;
            public string value;
        }

        public struct XMLOut
        {
            public Int64 descentriescount;
            public List<Entry> entries;
            public Int64 languagesentriescount;
            public List<Language> languages;
        }

        private static List<DescArray> ReadIndexedStrings(Int64 startingoffset, Int64 elementcount, BinaryReader binaryReader)
        {
            List<DescArray> descentries = new List<DescArray>();

            for (Int64 i = 0; i < elementcount; i++)
            {
                binaryReader.BaseStream.Seek(startingoffset + i * 8, SeekOrigin.Begin);
                Int64 address = binaryReader.ReadInt64();
                DescArray nuovo = new DescArray();
                binaryReader.BaseStream.Seek(address, SeekOrigin.Begin);
                nuovo.itemcount = binaryReader.ReadInt64();
                nuovo.stringpointers = new Int64[nuovo.itemcount];
                Int64 j = 0;
                nuovo.type_always_0x10 = binaryReader.ReadInt64();
                for (; j < nuovo.itemcount; j++)
                    nuovo.stringpointers[j] = binaryReader.ReadInt64();
                nuovo.strings = new StringCtr[nuovo.itemcount];
                for (j = 0; j < nuovo.itemcount; j++)
                {
                    StringCtr str = new StringCtr();
                    str.index = binaryReader.ReadInt64();
                    str.type_always_0x10 = binaryReader.ReadInt64();
                    Int64 curraddr = binaryReader.BaseStream.Position;
                    int stringlength = 0;
                    while (binaryReader.ReadByte() != 0)
                    {
                        stringlength++;
                    }
                    binaryReader.BaseStream.Seek(curraddr, SeekOrigin.Begin);
                    str.value = Encoding.GetEncoding(EncodingCodePage).GetString(binaryReader.ReadBytes(stringlength));
                    binaryReader.BaseStream.Position += 1; //Take the null termination into account
                    nuovo.strings[j] = str;
                }
                descentries.Add(nuovo);
            }
            return descentries;
        }

        private static List<string> ReadRawPointedStrings(Int64 startingoffset, Int64 elementcount, BinaryReader binaryReader)
        {
            List<string> textentries = new List<string>();
            for (Int64 i = 0; i < elementcount; i++)
            {
                binaryReader.BaseStream.Seek(startingoffset + i * 8, SeekOrigin.Begin);
                Int64 address = binaryReader.ReadInt64();
                int stringlength = 0;
                binaryReader.BaseStream.Seek(address, SeekOrigin.Begin);
                while (binaryReader.ReadByte() != 0)
                {
                    stringlength++;
                }
                binaryReader.BaseStream.Seek(address, SeekOrigin.Begin);
                textentries.Add(Encoding.GetEncoding(EncodingCodePage).GetString(binaryReader.ReadBytes(stringlength)));
            }
            return textentries;
        }

        private static List<Entry> BuildEntries(List<string> textentries, List<DescArray> entrydescriptions, Int64 entrycount, bool reorderbyfilewrite)
        {
            List<Entry> entries = new List<Entry>();
            for (int i = 0; i < entrydescriptions.Count; i++)
            {
                for (int j = 0; j < entrydescriptions[i].itemcount; j++)
                {
                    string current = entrydescriptions[i].strings[j].value;
                    int index = (int)entrydescriptions[i].strings[j].index;
                    List<string> multiple = new List<string>();
                    while (index < textentries.Count)
                    {
                        multiple.Add(textentries[index]);
                        index += (int)entrycount;
                    }
                    Entry nuovo = new Entry();
                    nuovo.desc = current;
                    nuovo.values = multiple;
                    nuovo.descindex = (int)entrydescriptions[i].strings[j].index;
                    nuovo.desccontainerindex = i;
                    entries.Add(nuovo);
                }
            }
            if (reorderbyfilewrite)
            {
                entries = entries.OrderBy(x => x.descindex).ToList();
            }
            return entries;
        }

        private static List<Language> BuildLangData(List<DescArray> langentries)
        {
            List<Language> languagedata = new List<Language>();
            for (int i = 0; i < langentries.Count; i++)
            {
                for (int j = 0; j < langentries[i].itemcount; j++)
                {
                    string current = langentries[i].strings[j].value;
                    int index = (int)langentries[i].strings[j].index;
                    Language nuovo = new Language();
                    nuovo.langcontainerindex = i;
                    nuovo.langindex = index;
                    nuovo.value = current;
                    languagedata.Add(nuovo);
                }
            }
            return languagedata;
        }

        public static void LTBExtract(string filein, string fileout)
        {
            FileStream fs = new FileStream(filein, FileMode.Open);
            BinaryReader binaryReader = new BinaryReader(fs);
            LtbStruct ltb = ByteToType<LtbStruct>(binaryReader);
            XMLOut output = new XMLOut();

            List<string> textentries = ReadRawPointedStrings(ltb.textaddressoffset, ltb.textelemcount, binaryReader);
            List<DescArray> entrydescriptions = ReadIndexedStrings(ltb.descaddressoffset, ltb.desccount, binaryReader);

            output.descentriescount = ltb.desccount;
            output.entries = BuildEntries(textentries, entrydescriptions, ltb.uniqueentrycount, true);

            List<DescArray> descentries = ReadIndexedStrings(ltb.languagecontainersoffset, ltb.languagecontainerscount, binaryReader);

            output.languagesentriescount = ltb.languagecontainerscount;
            output.languages = BuildLangData(descentries);

            fs.Close();
            binaryReader.Dispose();

            fs = new FileStream(fileout, FileMode.Create);
            XmlWriterSettings xmlset = new XmlWriterSettings();
            xmlset.Indent = true;
            xmlset.NewLineHandling = NewLineHandling.Entitize;
            XmlWriter xr = XmlWriter.Create(fs, xmlset);
            XmlSerializer serializer = new XmlSerializer(typeof(XMLOut));
            serializer.Serialize(xr, output);
            fs.Close();
        }

        public static void LTBRepack(string filein, string fileout)
        {
            XMLOut cont;
            XmlReaderSettings set = new XmlReaderSettings();
            FileStream fs = new FileStream(filein, FileMode.Open);
            XmlReader xr = XmlReader.Create(fs);
            XmlSerializer serializer = new XmlSerializer(typeof(XMLOut));
            try
            {
                cont = (XMLOut)serializer.Deserialize(xr);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            fs.Close();

            //Sanity check
            for (int i = 0; i < cont.entries.Count; i++)
            {
                if (cont.entries[i].values.Count != cont.languages.Count)
                {
                    throw new Exception("The entry with description \'" + cont.entries[i].desc + "\' has " + cont.entries[i].values.Count + " language values compared to the " + cont.languages.Count + " specified at the end of the document. Please fix the XML file.");
                }
            }


            fs = new FileStream(fileout, FileMode.Create);
            BinaryWriter binaryWriter = new BinaryWriter(fs);
            LtbStruct repacked = new LtbStruct();
            repacked.unk4_0000 = 0;
            repacked.uniqueentrycount = cont.entries.Count;
            repacked.textelemcount = cont.entries.Count * cont.languages.Count;
            repacked.textaddressoffset = 0x40;
            binaryWriter.Write(new byte[Marshal.SizeOf(repacked)], 0, Marshal.SizeOf(repacked));
            Int64 StringsIndexesPosition = fs.Position;
            binaryWriter.Write(new byte[repacked.textelemcount * 8], 0, (int)repacked.textelemcount * 8);
            PadBytes(binaryWriter, 64);

            for (int i = 0; i < cont.languages.Count; i++)
            {
                for (int j = 0; j < repacked.uniqueentrycount; j++)
                {
                    Int64 RawPointedStringsPosition = fs.Position;
                    fs.Seek(StringsIndexesPosition, SeekOrigin.Begin);
                    binaryWriter.Write(RawPointedStringsPosition);
                    StringsIndexesPosition = fs.Position;
                    fs.Seek(RawPointedStringsPosition, SeekOrigin.Begin);
                    binaryWriter.Write(Encoding.GetEncoding(EncodingCodePage).GetBytes(cont.entries[j].values[i]));
                    binaryWriter.Write((byte)0);
                    PadBytes(binaryWriter, 8);
                }
            }

            PadBytes(binaryWriter, 64);

            repacked.desccount = cont.descentriescount;
            repacked.descaddressoffset = fs.Position;
            Int64 DescIndexesPosition = fs.Position;
            binaryWriter.Write(new byte[repacked.desccount * 8], 0, (int)repacked.desccount * 8);
            PadBytes(binaryWriter, 64);
            MemoryStream ms;
            BinaryWriter binaryWritersmall;
            for (int i = 0; i < repacked.desccount; i++)
            {
                ms = new MemoryStream();
                binaryWritersmall = new BinaryWriter(ms);
                List<Entry> filtered = cont.entries.FindAll(x => x.desccontainerindex == i).ToList();
                DescArray descArray = new DescArray();
                descArray.itemcount = filtered.Count;
                descArray.type_always_0x10 = 0x10;
                binaryWritersmall.Write(descArray.itemcount);
                binaryWritersmall.Write(descArray.type_always_0x10);
                Int64 OffsetPosition = ms.Position;
                StringsIndexesPosition = ms.Position;
                binaryWritersmall.Write(new byte[descArray.itemcount * 8], 0, (int)descArray.itemcount * 8);
                for (int j = 0; j < descArray.itemcount; j++)
                {
                    binaryWritersmall.Write(filtered[j].descindex);
                    binaryWritersmall.Write(descArray.type_always_0x10);
                    Int64 RawPointedStringsPosition = ms.Position;
                    ms.Seek(StringsIndexesPosition, SeekOrigin.Begin);
                    binaryWritersmall.Write(RawPointedStringsPosition - OffsetPosition);
                    StringsIndexesPosition = ms.Position;
                    ms.Seek(RawPointedStringsPosition, SeekOrigin.Begin);
                    binaryWritersmall.Write(Encoding.GetEncoding(EncodingCodePage).GetBytes(filtered[j].desc));
                    binaryWritersmall.Write((byte)0);
                }
                Int64 RawDescStringsPosition = fs.Position;
                fs.Seek(DescIndexesPosition, SeekOrigin.Begin);
                binaryWriter.Write(RawDescStringsPosition);
                DescIndexesPosition = fs.Position;
                fs.Seek(RawDescStringsPosition, SeekOrigin.Begin);
                binaryWriter.Write(ms.ToArray());
                PadBytes(binaryWriter, 8);
                ms.Close();
                binaryWritersmall.Dispose();
            }

            PadBytes(binaryWriter, 64);

            repacked.languagecontainerscount = cont.languagesentriescount;
            repacked.languagecontainersoffset = fs.Position;
            DescIndexesPosition = fs.Position;

            binaryWriter.Write(new byte[repacked.languagecontainerscount * 8], 0, (int)repacked.languagecontainerscount * 8);
            PadBytes(binaryWriter, 64);

            for (int i = 0; i < repacked.languagecontainerscount; i++)
            {
                ms = new MemoryStream();
                binaryWritersmall = new BinaryWriter(ms);
                List<Language> filtered = cont.languages.FindAll(x => x.langcontainerindex == i).ToList();
                DescArray descArray = new DescArray();
                descArray.itemcount = filtered.Count;
                descArray.type_always_0x10 = 0x10;
                binaryWritersmall.Write(descArray.itemcount);
                binaryWritersmall.Write(descArray.type_always_0x10);
                Int64 OffsetPosition = ms.Position;
                StringsIndexesPosition = ms.Position;
                binaryWritersmall.Write(new byte[descArray.itemcount * 8], 0, (int)descArray.itemcount * 8);
                for (int j = 0; j < descArray.itemcount; j++)
                {
                    binaryWritersmall.Write(filtered[j].langindex);
                    binaryWritersmall.Write(descArray.type_always_0x10);
                    Int64 RawPointedStringsPosition = ms.Position;
                    ms.Seek(StringsIndexesPosition, SeekOrigin.Begin);
                    binaryWritersmall.Write(RawPointedStringsPosition - OffsetPosition);
                    StringsIndexesPosition = ms.Position;
                    ms.Seek(RawPointedStringsPosition, SeekOrigin.Begin);
                    binaryWritersmall.Write(Encoding.GetEncoding(EncodingCodePage).GetBytes(filtered[j].value));
                    binaryWritersmall.Write((byte)0);
                }
                Int64 RawDescStringsPosition = fs.Position;
                fs.Seek(DescIndexesPosition, SeekOrigin.Begin);
                binaryWriter.Write(RawDescStringsPosition);
                DescIndexesPosition = fs.Position;
                fs.Seek(RawDescStringsPosition, SeekOrigin.Begin);
                binaryWriter.Write(ms.ToArray());
                PadBytes(binaryWriter, 8);
                ms.Close();
                binaryWritersmall.Dispose();
            }

            PadBytes(binaryWriter, 64);

            fs.Seek(0, SeekOrigin.Begin);
            TypeToByte<LtbStruct>(binaryWriter, repacked);
            fs.Close();
        }
    }
}

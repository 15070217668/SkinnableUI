﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO.Packaging;
using System.IO;
using System.IO.Compression;

namespace UnusefulPlayer
{
    public class SerializationHelper
    {
        public static readonly Uri PKG_SKIN_FILE = new Uri(@"/skin.xml", UriKind.Relative);
        public static readonly string PKG_RES_PREFIX = @"/_res__";

        public static void SaveSkinPackage(string fileName, Skin skin)
        {
            using (var pack = ZipPackage.Open(fileName, FileMode.Create))
            {
                var part = pack.CreatePart(PKG_SKIN_FILE, System.Net.Mime.MediaTypeNames.Text.Xml, CompressionOption.Maximum);
                skin.Xml.Save(part.GetStream());

                foreach (var item in skin.Resources)
                {
                    var item_uri = new Uri(item.Key, UriKind.Relative);
                    var item_part = pack.CreatePart(item_uri, "", CompressionOption.Maximum);
                    item.Value.WriteTo(item_part.GetStream());
                }
            }
        }

        public static Skin OpenSkinPackage(string fileName)
        {
            var resources = new Dictionary<string, MemoryStream>();
            
            var p = ZipPackage.Open(fileName);

            var doc = new XmlDocument();
            doc.Load(XmlReader.Create(p.GetPart(PKG_SKIN_FILE).GetStream()));

            foreach (var part in p.GetParts())
            {
                var pname = part.Uri.OriginalString;
                if (pname.StartsWith(PKG_RES_PREFIX))
                {
                    var s = new MemoryStream();
                    part.GetStream().CopyTo(s);
                    resources.Add(pname, s);
                }
            }

            p.Close();

            return new Skin(doc, resources);
        }

        public static void LoadInteger(XmlElement element, string attribute, Action<int> setter)
        {
            if (element.HasAttribute(attribute))
                setter(int.Parse(element.GetAttribute(attribute), System.Globalization.NumberFormatInfo.InvariantInfo));
        }

        public static void LoadBoolean(XmlElement element, string attribute, Action<bool> setter)
        {
            if (element.HasAttribute(attribute))
                setter(XmlConvert.ToBoolean(element.GetAttribute(attribute)));
        }

        public static void LoadFloat(XmlElement element, string attribute, Action<float> setter)
        {
            if (element.HasAttribute(attribute))
                setter(float.Parse(element.GetAttribute(attribute), System.Globalization.NumberFormatInfo.InvariantInfo));
        }

        public static void LoadString(XmlElement element, string attribute, Action<string> setter)
        {
            if (element.HasAttribute(attribute))
                setter(element.GetAttribute(attribute));
        }

        public static void LoadEnum<T>(XmlElement element, string attribute, Action<T> setter)
        {
            if (element.HasAttribute(attribute))
                setter((T)Enum.Parse(typeof(T), element.GetAttribute(attribute), true));
        }

        public static void LoadColor(XmlElement element, string attribute, Action<System.Drawing.Color> setter)
        {
            if (element.HasAttribute(attribute))
            {
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(element.GetAttribute(attribute));
                var goodColor = System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
                setter(goodColor);
            }
        }

        public static void LoadFont(XmlElement element, string attribute, Action<System.Drawing.Font> setter)
        {
            if (element.HasAttribute(attribute))
                setter((System.Drawing.Font)new System.Drawing.FontConverter().ConvertFromInvariantString(element.GetAttribute(attribute)));
        }

        public static void LoadBitmapFromResources(XmlElement element, string attribute, Dictionary<String, System.IO.MemoryStream> resources, Action<System.Drawing.Bitmap> setter)
        {
            if (element.HasAttribute(attribute))
                setter(new System.Drawing.Bitmap(resources[element.GetAttribute(attribute)]));
        }



        public static void SetNinePatch(NinePatch p9, string nodeName, Dictionary<string, System.IO.MemoryStream> resources, System.Xml.XmlElement node)
        {
            if (p9 != null)
            {
                System.IO.MemoryStream m = new System.IO.MemoryStream();
                p9.Image.Save(m, System.Drawing.Imaging.ImageFormat.Png);

                String duplicateKey = (from r in resources
                                       where StreamsAreEqual(r.Value, m)
                                       select r.Key)
                                       .DefaultIfEmpty(null).FirstOrDefault();

                if (duplicateKey == null)
                { 
                    String filename = SerializationHelper.PKG_RES_PREFIX + resources.Count + ".png";
                    resources.Add(filename, m);
                    node.SetAttribute(nodeName, filename);
                }
                else
                {
                    node.SetAttribute(nodeName, duplicateKey);
                }
            }
        }

        static bool StreamsAreEqual(System.IO.MemoryStream first, System.IO.MemoryStream second)
        {
            const int BYTES_TO_READ = sizeof(Int64);

            if (first.Length != second.Length)
                return false;

            int iterations = (int)Math.Ceiling((double)first.Length / BYTES_TO_READ);

            byte[] one = new byte[BYTES_TO_READ];
            byte[] two = new byte[BYTES_TO_READ];

            for (int i = 0; i < iterations; i++)
            {
                first.Read(one, 0, BYTES_TO_READ);
                second.Read(two, 0, BYTES_TO_READ);

                if (BitConverter.ToInt64(one, 0) != BitConverter.ToInt64(two, 0))
                    return false;
            }

            return true;
        }
    }
}

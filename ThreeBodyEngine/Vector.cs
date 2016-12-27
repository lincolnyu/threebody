using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace ThreeBodyEngine
{
    public class Vector : IXmlSerializable
    {
        #region Fields

        public const string XmlElementName = "Vector";

        #endregion

        #region Properteis

        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public double SquareLength
        {
            get { return X*X + Y*Y + Z*Z; }
        }

        public double Length 
        {
            get { return Math.Sqrt(SquareLength); }
            set
            {
                var c = value / Length;
                X *= c;
                Y *= c;
                Z *= c;
            }
        }

        public bool IsZero
        {
            get { return SquareLength < double.Epsilon; }
        }

        #endregion

        #region Methods

        #region IXmlSerializable

        public XmlSchema GetSchema()
        {
            throw new NotImplementedException();
        }

        public void ReadXml(XmlReader reader)
        {
            var elementRead = false;
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name == XmlElementName && !elementRead)
                        {
                            elementRead = true;
                            var isEmpty = reader.IsEmptyElement;

                            var has = reader.MoveToFirstAttribute();
                            while (has)
                            {
                                switch (reader.Name)
                                {
                                    case "X":
                                        X = double.Parse(reader.Value);
                                        break;
                                    case "Y":
                                        Y = double.Parse(reader.Value);
                                        break;
                                    case "Z":
                                        Z = double.Parse(reader.Value);
                                        break;
                                }
                                has = reader.MoveToNextAttribute();
                            }

                            if (isEmpty)
                            {
                                return; //finished
                            }
                            break;
                        }
                        throw new XmlException("Unexpected element");
                    case XmlNodeType.EndElement:
                        if (reader.Name == XmlElementName)
                        {
                            return; // finished
                        }
                        throw new XmlException("Unexpected end element");
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("Vector");
            writer.WriteAttributeString("X", X.ToString());
            writer.WriteAttributeString("Y", Y.ToString());
            writer.WriteAttributeString("Z", Z.ToString());
            writer.WriteEndElement();
        }

        #endregion

        public Vector Add(Vector other)
        {
            var v = new Vector
            {
                X = X + other.X,
                Y = Y + other.Y,
                Z = Z + other.Z
            };
            return v;
        }

        public Vector Sub(Vector other)
        {
            var v = new Vector
            {
                X = X - other.X,
                Y = Y - other.Y,
                Z = Z - other.Z
            };
            return v;
        }

        public Vector Scale(double s)
        {
            var v = new Vector
            {
                X = X*s,
                Y = Y*s,
                Z = Z*s
            };
            return v;
        }

        public void AddToSelf(Vector other)
        {
            X += other.X;
            Y += other.Y;
            Z += other.Z;
        }

        public void SubtractFromSelf(Vector other)
        {
            X -= other.X;
            Y -= other.Y;
            Z -= other.Z;
        }

        public double CrossProduct(Vector other)
        {
            var cp = X*other.X + Y*other.Y + Z*other.Z;
            return cp;
        }

        public Vector Clone()
        {
            return new Vector
            {
                X = X,
                Y = Y,
                Z = Z
            };
        }

        public void CopyFrom(Vector other)
        {
            X = other.X;
            Y = other.Y;
            Z = other.Z;
        }

        #endregion
    }
}

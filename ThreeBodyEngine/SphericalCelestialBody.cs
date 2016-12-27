using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace ThreeBodyEngine
{
    public class SphericalCelestialBody : IXmlSerializable
    {
        #region Fields

        public const string XmlElementName = "Body";

        #endregion

        #region Constructors

        public SphericalCelestialBody()
        {
            Position = new Vector();
            Velocity = new Vector();
        }

        #endregion

        #region Properties

        /// <summary>
        ///  in kg (for exporting, 1E30 kg as 1.0)
        /// </summary>
        public double Mass { get; set; }

        /// <summary>
        ///  in m (for exporting 1E9 m as 1.0)
        /// </summary>
        public double Radius
        {
            get;
            set;
        }

        /// <summary>
        ///  Colour this body visualizes in
        /// </summary>
        public int Argb { get; set; }

        /// <summary>
        ///  The sun's as 1.0
        /// </summary>
        // TODO visualize it later
        public double Brightness
        {
            get; set; 
        }
    
        // TODO spectrum

        /// <summary>
        ///  in m (for exporting 1E12 m as 1.0)
        /// </summary>
        public Vector Position { get; set; }

        /// <summary>
        ///  in m/s (for exporting 1E3 m/s as 1.0)
        /// </summary>
        public Vector Velocity { get; set; } 

        #endregion

        #region Methods

        #region IXmlSerializable members

        public XmlSchema GetSchema()
        {
            throw new System.NotImplementedException();
        }

        public void ReadXml(XmlReader reader)
        {
            var elementRead = false;
            var readingState = 0;
            do
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (readingState != 0)
                        {
                            throw new XmlException("Unexpected element");
                        }
                        switch (reader.Name)
                        {
                            case XmlElementName:
                                if (!elementRead)
                                {
                                    elementRead = true;
                                    var isEmpty = reader.IsEmptyElement;

                                    var has = reader.MoveToFirstAttribute();
                                    while (has)
                                    {
                                        switch (reader.Name)
                                        {
                                            case "Mass":
                                            {
                                                var mass = double.Parse(reader.Value);
                                                Mass = mass*1E30;
                                                break;
                                            }
                                            case "Radius":
                                            {
                                                var radius = double.Parse(reader.Value);
                                                Radius = radius*1E9;
                                                break;
                                            }
                                            case "Color":
                                                Argb = int.Parse(reader.Value, NumberStyles.HexNumber);
                                                break;
                                        }
                                        has = reader.MoveToNextAttribute();
                                    }

                                    if (isEmpty)
                                    {
                                        return; //finished
                                    }
                                }
                                else
                                {
                                    throw new XmlException("Unexpected element");
                                }
                                break;
                            case XmlElementName + ".Position":
                            {
                                Position.ReadXml(reader); // in 1E12 m
                                Position.Scale(1E12);   // convert to m
                                readingState = 1;
                                break;
                            }
                            case XmlElementName + ".Velocity":
                            {
                                Velocity.ReadXml(reader); // in km/s
                                Velocity.Scale(1E3); // convert to m/s
                                readingState = 2;
                                break;
                            }
                            default:
                                throw new XmlException("Unexpected element");
                        }
                        break;
                    case XmlNodeType.EndElement:
                    {
                        var valid = false;
                        switch (reader.Name)
                        {
                            case XmlElementName:
                                if (readingState == 0)
                                {
                                    return;
                                }
                                break;
                            case XmlElementName + ".Position":
                                valid = readingState == 1;
                                break;
                            case XmlElementName + ".Velocity":
                                valid = readingState == 2;
                                break;
                        }
                        readingState = 0;
                        if (!valid)
                        {
                            throw new XmlException("Unexpected end element");
                        }
                        break;
                    }
                }

            } while (reader.Read());
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(XmlElementName);
            writer.WriteAttributeString("Mass", (Mass*1E-30).ToString());
            writer.WriteAttributeString("Radius", (Radius*1E-9).ToString());
            var colorString = Argb.ToString("x8");
            writer.WriteAttributeString("Color", colorString);
            
            writer.WriteStartElement(XmlElementName + ".Position");
            var pos = Position.Clone();
            pos.Scale(1E-12);
            pos.WriteXml(writer);
            writer.WriteEndElement();

            writer.WriteStartElement(XmlElementName + ".Velocity");
            var vel = Velocity.Clone();
            vel.Scale(1E-3);
            vel.WriteXml(writer);
            writer.WriteEndElement();

            writer.WriteEndElement();
        }

        #endregion

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace ThreeBodyEngine
{
    /// <summary>
    ///  a system of stars and planets
    /// </summary>
    /// <remarks>
    ///  Use this excellent resource for real system
    ///  http://ssd.jpl.nasa.gov/horizons.cgi
    /// </remarks>
    public class PlanetarySystem : IXmlSerializable
    {
        #region Fields

        public const string XmlElementName = "System";

        public const double GravitationalConstant = 6.673E-11; // m^3/(kg*s^2)

        private int _recordCountDown;
        private double _lastTime;
        private int _historyInterval;

        #endregion

        #region Constructors

        public PlanetarySystem()
        {
            Bodies = new List<SphericalCelestialBody>();
            RecommendedTimeStep = 60;

            InitializeHistory();
        }

        #endregion

        #region Properties

        /// <summary>
        ///  in seconds
        /// </summary>
        public double RecommendedTimeStep { get; set; }

        public List<SphericalCelestialBody> Bodies { get; private set; }

        public bool KeepHistory
        {
            get { return HistoryInterval > 0; }
        }
        public List<TimeSpan> Times { get; private set; } 
        public List<List<BodyRecord>> BodyHistories { get; private set; }

        public int HistoryInterval
        {
            get { return _historyInterval; }
            set
            {
                if (_historyInterval != value)
                {
                    _historyInterval = value;
                    _recordCountDown = 0;
                }
            }
        }

        #endregion

        #region Methods

        #region IXmlSerializable members

        public XmlSchema GetSchema()
        {
            throw new NotImplementedException();
        }

        public void ReadXml(XmlReader reader)
        {
            var elementRead = false;
            var bodyReadStage = 0;
            var finished = false;
            Bodies.Clear();
            while (reader.Read() && !finished)
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Name)
                        {
                            case XmlElementName:
                                if (elementRead)
                                {
                                    throw new XmlException("Unexpected element");
                                }
                                elementRead = true;
                                var isEmpty = reader.IsEmptyElement;

                                var has = reader.MoveToFirstAttribute();
                                while (has)
                                {
                                    switch (reader.Name)
                                    {
                                        case "TimeStep":
                                            RecommendedTimeStep = double.Parse(reader.Value);
                                            break;
                                    }
                                    has = reader.MoveToNextAttribute();
                                }

                                if (isEmpty)
                                {
                                    finished = true;
                                }
                                break;
                            case XmlElementName + ".Bodies":
                                if (bodyReadStage == 0)
                                {
                                    bodyReadStage = 1;
                                }
                                else
                                {
                                    throw new XmlException("Unexpected element");
                                }
                                break;
                            case SphericalCelestialBody.XmlElementName:
                            {
                                if (bodyReadStage != 1)
                                {
                                    throw new XmlException("Unexpected element");
                                }
                                var body = new SphericalCelestialBody();
                                body.ReadXml(reader);
                                Bodies.Add(body);
                                break;
                            }
                            default:
                                throw new XmlException("Unexpected element");
                        }
                        break;
                    case XmlNodeType.EndElement:
                        switch (reader.Name)
                        {
                            case XmlElementName + ".Bodies":
                                if (bodyReadStage == 1)
                                {
                                    bodyReadStage = 2;
                                }
                                else
                                {
                                    throw new XmlException("Unexpected e");
                                }
                                break;
                            case XmlElementName:
                                if (bodyReadStage == 0 || bodyReadStage == 2)
                                {
                                    finished = true;
                                    break;
                                }
                                throw new XmlException("Unexpected e");
                        }
                        break;
                }
            }

            InitializeHistory();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(XmlElementName);
            writer.WriteAttributeString("TimeStep", RecommendedTimeStep.ToString());

            writer.WriteStartElement(XmlElementName + ".Bodies");

            foreach (var body in Bodies)
            {
                body.WriteXml(writer);
            }

            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        #endregion

        public void InitializeHistory()
        {
            Times = new List<TimeSpan>();
            BodyHistories = new List<List<BodyRecord>>();
            for (var i = 0; i < Bodies.Count; i++)
            {
                var history = new List<BodyRecord>();
                BodyHistories.Add(history);
            }
            _recordCountDown = 0;
            _lastTime = 0.0;
        }

        public void ClearHistory()
        {
            Times.Clear();
            foreach (var b in BodyHistories)
            {
                b.Clear();
            }
            _recordCountDown = 0;
            _lastTime = 0.0;
        }

        private List<Vector> GetAccelerations(double time)
        {
            var accs = new List<Vector>();
            var midPoints = new List<Vector>();

            for (var i = 0; i < Bodies.Count; i++)
            {
                var p1 = Bodies[i].Position;
                var p2 = p1.Add(Bodies[i].Velocity.Scale(time));
                var pm = p1.Add(p2).Scale(0.5);
                midPoints.Add(pm);
            }

            for (var i = 0; i < Bodies.Count; i++)
            {
                var bodyM = midPoints[i];
                var acc = new Vector();
                for (var j = 0; j < Bodies.Count; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    var other = Bodies[j];
                    var otherM = midPoints[j];

                    var v = otherM.Sub(bodyM);
                    var dd = v.SquareLength;

                    var c = other.Mass / dd;

                    var av = GravitationalConstant * c;
                    v.Length = av; // become acceleration

                    acc.AddToSelf(v);
                }

                accs.Add(acc);
            }

            return accs;
        }

        private List<Vector> GetAccelerations()
        {
            var accs = new List<Vector>();
            foreach (var body in Bodies)
            {
                var acc = new Vector();
                foreach (var other in Bodies)
                {
                    if (other == body)
                    {
                        continue;
                    }
                    var v = other.Position.Sub(body.Position);
                    var dd = v.SquareLength;

                    var c = other.Mass / dd;

                    var av = GravitationalConstant * c;
                    v.Length = av; // become acceleration

                    acc.AddToSelf(v);
                }
                accs.Add(acc);
            }

            return accs;
        }

        public bool SimulateStepBack(double time)
        {
            RecordBodies(-time);

            var accs = GetAccelerations();

            var edges = new List<Edge>();
            var i = 0;
            foreach (var body in Bodies)
            {
                var acc = accs[i]; // in 1E-5 m/s^2 i.e. 1E-8 km/s^2

                var oldPosition = body.Position.Clone();
                if (acc.IsZero)
                {
                    UpdatePosition(body.Position, body.Velocity, -time);
                }
                else
                {
                    var dv = acc.Scale(time * 1E-8);
                    var dv2 = dv.Scale(0.5);
                    var tempv = body.Velocity.Add(dv2);
                    UpdatePosition(body.Position, tempv, -time);

                    body.Velocity.SubtractFromSelf(dv);
                }

                edges.Add(new Edge
                {
                    P1 = oldPosition,
                    P2 = body.Position,
                    R = body.Radius * 1E-3
                });

                // collection detection
#if true
                if (!CheckEdges(edges))
                {
                    return false;
                }
#endif
                i++;
            }
            return true;
        }

        /// <summary>
        ///  Simulate one time step forward
        /// </summary>
        /// <param name="time">The time to simulate over, in sec</param>
        /// <returns>True if the simulation is stable and no bodies are suspected to collide</returns>
        public bool SimulateStep(double time)
        {
            RecordBodies(time);

            var accs = GetAccelerations(time);// GetAccelerations(time);

            var result = true;
            var edges = new List<Edge>();
            var i = 0;

            foreach (var body in Bodies)
            {
                var acc = accs[i]; // in 1E-5 m/s^2 i.e. 1E-8 km/s^2

                var oldPosition = body.Position.Clone();
                if (acc.IsZero)
                {
                    UpdatePosition(body.Position, body.Velocity, time);
                }
                else
                {
                    var dv = acc.Scale(time);
                    var dv2 = dv.Scale(0.5);
                    var tempv = body.Velocity.Add(dv2);
                    UpdatePosition(body.Position, tempv, time);

                    body.Velocity.AddToSelf(dv);
                }

                edges.Add(new Edge
                {
                    P1 = oldPosition,
                    P2 = body.Position,
                    R = body.Radius
                });

                // collision detection
#if true
                if (!CheckEdges(edges))
                {
                    result = false;
                }
#endif
                i++;
            }
            return result;
        }

        /// <summary>
        ///  Record the states of the bodies
        /// </summary>
        private void RecordBodies(double time)
        {
            if (!KeepHistory)
            {
                return;
            }

            if (_recordCountDown <= 0)
            {
                var recordTime = TimeSpan.FromSeconds(_lastTime);
                Times.Add(recordTime);
                lock (BodyHistories)
                {
                    for (var i = 0; i < Bodies.Count; i++)
                    {
                        var body = Bodies[i];
                        var record = new BodyRecord();
                        record.CopyFrom(body);
                        BodyHistories[i].Add(record);
                    }
                }

                _recordCountDown = HistoryInterval-1;
            }
            else
            {
                _recordCountDown--;
            }

            _lastTime += time;
        }

        private bool CheckEdges(IReadOnlyList<Edge> edges)
        {
            for (var i = 0; i < edges.Count - 1; i++)
            {
                for (var j = i + 1; j < edges.Count; j++)
                {
                    var e1 = edges[i];
                    var e2 = edges[j];
                    var minx1 = Math.Min(e1.P1.X - e1.R, e1.P2.X - e1.R);
                    var miny1 = Math.Min(e1.P1.Y - e1.R, e1.P2.Y - e1.R);
                    var minz1 = Math.Min(e1.P1.Z - e1.R, e1.P2.Z - e1.R);
                    var maxx1 = Math.Max(e1.P1.X + e1.R, e1.P2.X + e1.R);
                    var maxy1 = Math.Max(e1.P1.Y + e1.R, e1.P2.Y + e1.R);
                    var maxz1 = Math.Max(e1.P1.Z + e1.R, e1.P2.Z + e1.R);

                    var minx2 = Math.Min(e2.P1.X - e2.R, e2.P2.X - e2.R);
                    var miny2 = Math.Min(e2.P1.Y - e2.R, e2.P2.Y - e2.R);
                    var minz2 = Math.Min(e2.P1.Z - e2.R, e2.P2.Z - e2.R);
                    var maxx2 = Math.Max(e2.P1.X + e2.R, e2.P2.X + e2.R);
                    var maxy2 = Math.Max(e2.P1.Y + e2.R, e2.P2.Y + e2.R);
                    var maxz2 = Math.Max(e2.P1.Z + e2.R, e2.P2.Z + e2.R);
                    if (minx1 > maxx2) continue;
                    if (minx2 > maxx1) continue;
                    if (miny1 > maxy2) continue;
                    if (miny2 > maxy1) continue;
                    if (minz1 > maxz2) continue;
                    if (minz2 > maxz1) continue;

                    // TODO finer checking

                    return false;
                }
            }
            return true;
        }

        /// <summary>
        ///  Updates position
        /// </summary>
        /// <param name="pos">position in m</param>
        /// <param name="v">velocity in m/s</param>
        /// <param name="time">time passed in sec</param>
        private static void UpdatePosition(Vector pos, Vector v, double time)
        {
            var ds = v.Scale(time);
            pos.AddToSelf(ds);
        }

        #endregion
    }
}


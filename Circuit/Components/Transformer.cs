﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{ 
    /// <summary>
    /// Ideal transformer.
    /// </summary>
    [CategoryAttribute("Standard")]
    [DisplayName("Transformer")]
    public class Transformer : Component
    {
        private Terminal pa, pc, sa, sc;

        public override IEnumerable<Terminal> Terminals
        {
            get 
            {
                yield return pa;
                yield return pc;
                yield return sa;
                yield return sc;
            }
        }

        protected Ratio turns = new Ratio(1, 1);
        [Description("Turns ratio.")]
        [SchematicPersistent]
        public Ratio Turns { get { return turns; } set { turns = value; NotifyChanged("Turns"); } }

        public Transformer()
        {
            pa = new Terminal(this, "PA");
            pc = new Terminal(this, "PC");
            sa = new Terminal(this, "SA");
            sc = new Terminal(this, "SC");
            Name = "TX1"; 
        }

        public override void Analyze(ModifiedNodalAnalysis Mna) 
        {
            Expression Ip = Mna.AddNewUnknown("i" + Name + "p");
            pa.i = Ip;
            pc.i = -Ip;
            Expression Is = Mna.AddNewUnknown("i" + Name + "s");
            sa.i = -Is;
            sc.i = Is;
            Mna.AddEquation(Ip * turns, Is);

            Expression Vp = pa.V - pc.V;
            Expression Vs = sa.V - sc.V;
            Mna.AddEquation(Vp, turns * Vs);
        }

        public override void LayoutSymbol(SymbolLayout Sym)
        {
            Sym.AddTerminal(pa, new Coord(-10, 20));
            Sym.AddTerminal(pc, new Coord(-10, -20));
            Sym.AddTerminal(sa, new Coord(10, 20));
            Sym.AddTerminal(sc, new Coord(10, -20));

            Sym.AddWire(pa, new Coord(-10, 16));
            Sym.AddWire(pc, new Coord(-10, -16));
            Sym.AddWire(sa, new Coord(10, 16));
            Sym.AddWire(sc, new Coord(10, -16));
            Sym.InBounds(new Coord(-20, 0), new Coord(20, 0));

            float t1 = -3.0f * 3.141592f;
            float t2 = 4.0f * 3.141592f;
            float coil = 1.5f;

            float y1 = t1 + coil * (float)Math.Cos(t1);
            float y2 = t2 + coil * (float)Math.Cos(t2);

            Sym.DrawFunction(
                EdgeType.Black,
                (t) => -10.0f - (float)Math.Sin(t) * 4.0f,
                (t) => ((t + coil * (float)Math.Cos(t)) - y1) / (y2 - y1) * 32.0f - 16.0f,
                t1, t2, 64);

            Sym.DrawLine(EdgeType.Black, new Coord(-2, 16), new Coord(-2, -16));
            Sym.DrawLine(EdgeType.Black, new Coord(2, 16), new Coord(2, -16));

            Sym.DrawFunction(
                EdgeType.Black,
                (t) => 10.0f + (float)Math.Sin(t) * 4.0f,
                (t) => ((t + coil * (float)Math.Cos(t)) - y1) / (y2 - y1) * 32.0f - 16.0f,
                t1, t2, 64);

            Sym.DrawText(Name, new Coord(-16, 0), Alignment.Far, Alignment.Center);
            Sym.DrawText(Turns.ToString(), new Coord(16, 0), Alignment.Near, Alignment.Center);
        }

        public override string ToString() { return Name + " = " + Turns.ToString(); }
    }
}
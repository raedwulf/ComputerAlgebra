﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;
using SyMath;

namespace Circuit
{
    /// <summary>
    /// Simulate a circuit.
    /// </summary>
    public abstract class Simulation
    {
        // Expression for t at the previous timestep.
        protected static readonly Variable t0 = TransientSolution.t0;
        protected static readonly Variable t = TransientSolution.t;
        
        // This is used often enough to shorten it a few characters.
        protected static readonly Arrow t_t0 = Arrow.New(t, t0);

        private long n = 0;
        /// <summary>
        /// Get which sample the simulation is at.
        /// </summary>
        public long At { get { return n; } }
        /// <summary>
        /// Get the simulation time.
        /// </summary>
        public double Time { get { return At * TimeStep; } }

        /// <summary>
        /// Get the timestep for the simulation.
        /// </summary>
        public double TimeStep { get { return 1.0 / (double)SampleRate; } }

        private ILog log = new ConsoleLog();
        /// <summary>
        /// Get or set the log associated with this simulation.
        /// </summary>
        public ILog Log { get { return log; } set { log = value; } }

        private TransientSolution transient;
        /// <summary>
        /// The solution of the circuit we are simulating.
        /// </summary>
        public TransientSolution Transient { get { return transient; } }

        private int oversample;
        /// <summary>
        /// The oversampling factor for this simulation.
        /// </summary>
        public int Oversample { get { return oversample; } }

        /// <summary>
        /// The sampling rate of this simulation.
        /// </summary>
        public Quantity SampleRate { get { return Transient.SampleRate / oversample; } }

        /// <summary>
        /// Create a simulation for the given system solution.
        /// </summary>
        /// <param name="Transient">Transient solution to run.</param>
        /// <param name="Log">Log for simulation output.</param>
        public Simulation(TransientSolution Transient, int Oversample, ILog Log)
        {
            oversample = Oversample;
            transient = Transient;
            log = Log;
        }

        /// <summary>
        /// Clear all state from the simulation.
        /// </summary>
        public virtual void Reset()
        {
            n = 0;
        }

        // Where the magic happens...
        protected abstract void Process(
            long n, double T, int N,
            IEnumerable<KeyValuePair<Expression, double[]>> Input,
            IEnumerable<KeyValuePair<Expression, double[]>> Output,
            IEnumerable<Arrow> Arguments,
            int Oversample, int Iterations);

        /// <summary>
        /// Process some samples with this simulation.
        /// </summary>
        /// <param name="N">Number of samples to process.</param>
        /// <param name="Input">Mapping of node Expression -> double[] buffers that describe the input samples.</param>
        /// <param name="Output">Mapping of node Expression -> double[] buffers that describe requested output samples.</param>
        /// <param name="Arguments">Constant expressions describing the values of any parameters to the simulation.</param>
        public void Run(
            int N,
            IEnumerable<KeyValuePair<Expression, double[]>> Input,
            IEnumerable<KeyValuePair<Expression, double[]>> Output,
            IEnumerable<Arrow> Arguments,
            int Iterations)
        {
            // Call the implementation of process.
            Process(n, TimeStep, N, Input, Output, Arguments, Oversample, Iterations);

            // Check the last samples for infinity/NaN.
            foreach (KeyValuePair<Expression, double[]> i in Output)
            {
                double v = i.Value[i.Value.Length - 1];
                if (double.IsInfinity(v) || double.IsNaN(v))
                    throw new OverflowException("Simulation diverged after t=" + Quantity.ToString(Time, Units.s));
            }

            n += N;
        }

        private static Arrow[] NoArguments = new Arrow[] { };
        public void Run(
            int N,
            IEnumerable<KeyValuePair<Expression, double[]>> Input,
            IEnumerable<KeyValuePair<Expression, double[]>> Output, 
            int Iterations)
        {
            Run(N, Input, Output, NoArguments, Iterations);
        }

        public void Run(
            Expression InputNode, double[] InputSamples,
            IEnumerable<KeyValuePair<Expression, double[]>> Output, 
            int Iterations)
        {
            Run(
                InputSamples.Length,
                new KeyValuePair<Expression, double[]>[] { new KeyValuePair<Expression, double[]> (InputNode, InputSamples) },
                Output,
                Iterations);
        }

        public void Run(
            Expression InputNode, double[] InputSamples, 
            Expression OutputNode, double[] OutputSamples,
            int Iterations)
        {
            Run(
                InputSamples.Length,
                new KeyValuePair<Expression, double[]>[] { new KeyValuePair<Expression, double[]>(InputNode, InputSamples) },
                new KeyValuePair<Expression, double[]>[] { new KeyValuePair<Expression, double[]>(OutputNode, OutputSamples) },
                Iterations);
        }
    }
}

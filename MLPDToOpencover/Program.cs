using System;
using System.Collections.Generic;

using MonoDevelop.Profiler;

namespace MLPDToOpencover
{
    struct CoverageSummary
    {
        public int NumberSequencePoints { get; set; }
        public int VisitedSequencePoints { get; set; }
        public double SequenceCoverage { get; set; }
    }

    class ModuleCoverage
    {
        public string Name { get; set; }
        public string Filename { get; set; }
        public string Guid { get; set; }

        public Dictionary<string, uint> Files { get; }
        public Dictionary<string, ClassCoverage> Classes { get; }

        public CoverageSummary Summary { get; }

        public ModuleCoverage ()
        {
            Files = new Dictionary<string, uint> ();
            Classes = new Dictionary<string, ClassCoverage> ();
        }
    }

    class ClassCoverage
    {
        public string ClassName { get; set; }
        public Dictionary<string, MethodCoverage> Methods { get; }

        public CoverageSummary Summary { get; }

        public ClassCoverage ()
        {
            Methods = new Dictionary<string, MethodCoverage> ();
        }
    }

    class MethodCoverage
    {
        public string Name { get; set; }
        public ulong Token { get; set; }
        public uint FileRef { get; set; }
        public ulong MethodId { get; set; }
        public List<SequencePoint> SequencePoints { get; }
        public List<BranchPoint> BranchPoints { get; }
        public List<MethodPoint> MethodPoints { get; }

        public CoverageSummary Summary { get; }

        public MethodCoverage ()
        {
            SequencePoints = new List<SequencePoint> ();
            BranchPoints = new List<BranchPoint> ();
            MethodPoints = new List<MethodPoint> ();
        }
    }

    class SequencePoint
    {
        public ulong Count { get; set; }
        public ulong Uspid { get; set; }
        public ulong Ordinal { get; set; }
        public ulong Offset { get; set; }
        public ulong StartLine { get; set; }
        public ulong StartColumn { get; set; }
        public ulong EndLine { get; set; }
        public ulong EndColumn { get; set; }
    }

    class BranchPoint
    {
    }

    class MethodPoint
    {
    }

    class MainClass
    {
        static uint CurrentFileID = 1;

        public static void Main (string [] args)
        {
            var log = LogBuffer.Read (args [0]);

            var assemblyEvents = new List<CoverageAssemblyEvent> ();
            var classEvents = new List<CoverageClassEvent> ();
            var methodEvents = new List<CoverageMethodEvent> ();
            var statementEvents = new List<CoverageStatementEvent> ();

            foreach (var buffer in log.buffers) {
                foreach (var e in buffer.Events) {
                    if (e is CoverageAssemblyEvent) {
                        assemblyEvents.Add ((CoverageAssemblyEvent)e);
                    } else if (e is CoverageClassEvent) {
                        classEvents.Add ((CoverageClassEvent)e);
                    } else if (e is CoverageMethodEvent) {
                        methodEvents.Add ((CoverageMethodEvent)e);
                    } else if (e is CoverageStatementEvent) {
                        statementEvents.Add ((CoverageStatementEvent)e);
                    } else {
                        throw new InvalidProgramException ();
                    }
                }
            }

            ParseVisitedEvents (assemblyEvents, classEvents, methodEvents, statementEvents);

            Console.WriteLine ("Modules:");

        }

        static Dictionary<string, ModuleCoverage> ParseVisitedEvents (List<CoverageAssemblyEvent> assemblyEvents, List<CoverageClassEvent> classEvents, List<CoverageMethodEvent> methodEvents, List<CoverageStatementEvent> statementEvents)
        {
            Dictionary<string, ModuleCoverage> modules = new Dictionary<string, ModuleCoverage> ();
            Dictionary<ulong, MethodCoverage> idToMethod = new Dictionary<ulong, MethodCoverage> ();

            foreach (var e in assemblyEvents) {
                var m = new ModuleCoverage {
                    Name = e.Name,
                    Filename = e.Filename,
                    Guid = e.Guid
                };

                modules [m.Filename] = m;
            }

            foreach (var e in classEvents) {
                ModuleCoverage module;

                if (!modules.TryGetValue (e.Name, out module)) {
                    throw new InvalidProgramException ();
                }

                ClassCoverage c;
                if (module.Classes.TryGetValue (e.Class, out c)) {
                    continue;
                }

                c = new ClassCoverage {
                    ClassName = e.Class,
                };

                module.Classes [e.Class] = c;
            }

            foreach (var e in methodEvents) {
                ModuleCoverage module;

                if (!modules.TryGetValue (e.Assembly, out module)) {
                    throw new InvalidProgramException ();
                }

                ClassCoverage c;
                if (module.Classes.TryGetValue (e.Class, out c)) {
                    throw new InvalidProgramException ();
                }

                MethodCoverage method;

                if (c.Methods.TryGetValue (e.Name, out method)) {
                    continue;
                }

                uint fileID;
                if (!module.Files.TryGetValue (e.Filename, out fileID)) {
                    fileID = CurrentFileID++;

                    module.Files [e.Filename] = fileID;
                }

                method = new MethodCoverage {
                    Name = e.Name,
                    Token = e.Token,
                    MethodId = e.MethodId
                };

                c.Methods [e.Name] = method;
                idToMethod [method.MethodId] = method;
            }

            foreach (var e in statementEvents) {
                MethodCoverage method;

                if (!idToMethod.TryGetValue (e.MethodId, out method)) {
                    throw new InvalidProgramException ();
                }

                SequencePoint s = new SequencePoint {
                    Count = e.Counter,
                    Offset = e.Offset,
                    StartLine = e.Line,
                    StartColumn = e.Column
                };

                method.SequencePoints.Add (s);
            }

            return modules;
        }
    }
}

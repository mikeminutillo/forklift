using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace Forklift
{
    public class Env
    {
        public string Name { get; set; }
        public string ConnectionString { get; set; }
    }

    public class Plan
    {
        public string Name { get; set; }
        public Segment[] Segments { get; set; }

        public void UpdateAndCheck(IMetabase metabase)
        {
            // Load the Plans from the Segments
        }
    }

    public class PlanFile
    {
        private ILookup<string, Env> _environments;
        private ILookup<string, Plan> _plans;

        private PlanFile(IEnumerable<Env> environments, IEnumerable<Plan> plans)
	    {
            _environments = environments.ToLookup(x => x.Name, StringComparer.CurrentCultureIgnoreCase);
            _plans = plans.ToLookup(x => x.Name, StringComparer.CurrentCultureIgnoreCase);
	    }

        public Env Environment(string name)
        {
            var env =  _environments[name].FirstOrDefault();

            if(env != null)
                return env;

            Console.Error.WriteLine("No environment named " + name + " so assuming local sql express database with that name");

            return new Env
            {
                Name = name,
                ConnectionString = @"server=.\SQLEXPRESS;integrated security=sspi; database=" + name
            };
        }

        public Plan Plan(string name)
        {
            var plan = _plans[name].FirstOrDefault();

            if(plan != null)
                return plan;

            Console.Error.WriteLine("No plan found named " + name + " so assuming a table with that name");

            return new Plan
            {
                Name = name,
                Segments = Segment.Parse(name).ToArray()
            };
        }

        public static PlanFile Load(string planFile)
        {
            if (File.Exists(planFile) == false)
            {
                Console.Error.WriteLine("Plan file cannot be found. Specify with -plan <planfile>. Defaults to forklift.plan");
                return new PlanFile(Enumerable.Empty<Env>(), Enumerable.Empty<Plan>());
            }

            var plans = new List<Plan>();
            var envs = new List<Env>();
            
            var finishedEnvironments = false;
            var currentPlan = default(Plan);

            var sb = new StringBuilder();

            foreach (var line in File.ReadAllLines(planFile))
            {
                if (finishedEnvironments)
                {
                    var m = Regex.Match(line, @"^\[(.+)\]$");
                    if (m.Success)
                    {
                        if (currentPlan != default(Plan))
                        {
                            currentPlan.Segments = Segment.Parse(sb.ToString()).ToArray();
                            sb.Clear();
                        }

                        currentPlan = new Plan
                        {
                            Name = m.Result("$1")
                        };

                        plans.Add(currentPlan);
                    }
                    else
                    {
                        sb.AppendLine(line);
                    }
                }
                else
                {
                    if (String.IsNullOrWhiteSpace(line))
                    {
                        finishedEnvironments = true;
                        continue;
                    }

                    var match = Regex.Match(line, @"^(.+):(.+)$");
                    if (!match.Success)
                        throw new Exception(String.Format("Plan file must start with environments in 'name:connectionstring' format (without quotes). '{0}' does not match", line));

                    envs.Add(new Env
                    {
                        Name = match.Result("$1").Trim(),
                        ConnectionString = match.Result("$2").Trim()
                    });
                }

            }

            if (currentPlan != default(Plan))
            {
                currentPlan.Segments = Segment.Parse(sb.ToString()).ToArray();
            }

            return new PlanFile(envs, plans);
        }
    }
}

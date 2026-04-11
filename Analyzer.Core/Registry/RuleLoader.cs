using Analyzer.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Core.Registry
{
    public static class RuleLoader
    {
        public static List<IRule> LoadRules()
        {
            var rules = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return new Type[0]; }
                })
                .Where(t =>
                    typeof(IRule).IsAssignableFrom(t) &&
                    !t.IsAbstract &&
                    t.GetConstructor(Type.EmptyTypes) != null)
                .Select(t => (IRule)Activator.CreateInstance(t))
                .ToList();

            return rules;
        }

        public static List<IRule> LoadFromDll(string path)
        {
            var rules = new List<IRule>();

            var assembly = Assembly.LoadFrom(path);

            var types = assembly.GetTypes()
                .Where(t => typeof(IRule).IsAssignableFrom(t));

            foreach (var type in types)
            {
                rules.Add((IRule)Activator.CreateInstance(type));
            }

            return rules;
        }
    }
}

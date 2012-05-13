/*
Copyright (c) 2012 Fredrik Ehnbom

This software is provided 'as-is', without any express or implied
warranty. In no event will the authors be held liable for any damages
arising from the use of this software.

Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:

   1. The origin of this software must not be misrepresented; you must not
   claim that you wrote the original software. If you use this software
   in a product, an acknowledgment in the product documentation would be
   appreciated but is not required.

   2. Altered source versions must be plainly marked as such, and must not be
   misrepresented as being the original software.

   3. This notice may not be removed or altered from any source
   distribution.
*/
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class CompleteSharp
{
    private static string sep = ";;--;;";

    private static int GetParameterExtent(string parameter)
    {
        int hookcount = 0;
        int ltgtcount = 0;
        for (int i = 0; i < parameter.Length; i++)
        {
            switch (parameter[i])
            {
                case ']':
                {
                    hookcount--;
                    if (hookcount == 0 && ltgtcount == 0)
                    {
                        return i+1;
                    }
                    break;
                }
                case '[': hookcount++; break;
                case ',':
                {
                    if (parameter[i] == ',' && hookcount == 0 && ltgtcount == 0)
                    {
                        return i;
                    }
                    break;
                }
                case '<': ltgtcount++; break;
                case '>':
                {
                    ltgtcount--;
                    if (hookcount == 0 && ltgtcount == 0)
                    {
                        return i+1;
                    }
                    break;
                }
            }
        }
        return parameter.Length;
    }
    private static string[] SplitParameters(string parameters, bool fix=true)
    {
        List<string> s = new List<string>();
        for (int i = 0; i < parameters.Length;)
        {
            int len = GetParameterExtent(parameters.Substring(i));
            string toadd = parameters.Substring(i, len);
            while (toadd.Length >= 2 && toadd.StartsWith("["))
            {
                toadd = toadd.Substring(1, toadd.Length-2);
                toadd = toadd.Substring(0, GetParameterExtent(toadd));
            }
            if (fix)
                toadd = FixName(toadd);
            toadd = toadd.Trim();
            if (toadd.Length > 0)
                s.Add(toadd);
            i += len;
        }
        return s.ToArray();
    }

    private static string ParseParameters(string parameters, int expected, bool insertion)
    {
        if (parameters.Length >= 2 && parameters.StartsWith("["))
        {
            parameters = parameters.Substring(1, parameters.Length-2);
        }
        string[] para = null;
        if (parameters.Length > 0)
        {
            para = SplitParameters(parameters);
        }
        else
        {
            para = new string[expected];
            for (int i = 0; i < expected; i++)
            {
                para[i] = "T" + (i+1);
            }
        }
        string ret = "";
        for (int i = 0; i < para.Length; i++)
        {
            if (ret.Length > 0)
                ret += ", ";
            if (!insertion)
            {
                ret+= para[i];
            }
            else
            {
                ret += "${" + (i+1) + ":" + para[i] + "}";
            }
        }
        return ret;
    }
    private static string FixName(string str, bool insertion=false)
    {
        int index = str.IndexOf('`');
        if (index != -1)
        {
            Regex regex = new Regex("([\\w.]+)\\`(\\d+)(\\[.*\\])?");
            Match m = regex.Match(str);
            string type = m.Groups[1].ToString();
            int num = System.Int32.Parse(m.Groups[2].ToString());
            string parameters = m.Groups[3].ToString();
            string extra = "";
            while (parameters.EndsWith("[]"))
            {
                extra += "[]";
                parameters = parameters.Substring(0, parameters.Length-2);
            }

            return type + "<" + ParseParameters(parameters, num, insertion) + ">" + extra;
        }
        return str;
    }

    private static string[] GetTemplateArguments(string template)
    {
        int index = template.IndexOf('<');
        int index2 = template.LastIndexOf('>');
        if (index != -1 && index2 != -1)
        {
            string args = template.Substring(index+1, index2-index-1);
            return SplitParameters(args, false);
        }
        return new string[0];
    }

    private static string GetBase(string fullname)
    {
        int index = fullname.IndexOf('<');
        if (index == -1)
            return fullname;
        return fullname.Substring(0, index);
    }

    private static Type GetType(Assembly[] assemblies, string basename, string[] templateParam)
    {
        if (templateParam.Length > 0 && basename.IndexOf('`') == -1)
        {
            basename += "`" + templateParam.Length;
        }
        Type[] subtypes = new Type[templateParam.Length];
        for (int i = 0; i < subtypes.Length; i++)
        {
            string bn = GetBase(templateParam[i]);
            string[] args = GetTemplateArguments(templateParam[i]);
            subtypes[i] = GetType(assemblies, bn, args);
        }

        Type t = Type.GetType(basename);
        if (t == null)
        {
            foreach (Assembly ass in assemblies)
            {
                t = ass.GetType(basename);
                if (t != null)
                    break;
            }
        }
        if (t != null && subtypes.Length > 0)
        {
            try
            {
                Type t2 = t.MakeGenericType(subtypes);
                System.Console.Error.WriteLine("returning type2: " + t2.FullName);
                return t2;
            }
            catch (Exception e)
            {
                System.Console.Error.WriteLine(e.Message);
                System.Console.Error.WriteLine(e.StackTrace);
            }
        }
        if (t != null)
            System.Console.Error.WriteLine("returning type: " + t.FullName);
        return t;
    }
    private enum Accessibility
    {
        NONE = (0<<0),
        STATIC = (1<<0),
        PRIVATE = (1<<1),
        PROTECTED = (1<<2),
        PUBLIC = (1<<3),
        INTERNAL = (1<<4)
    };

    private static int GetModifiers(MemberInfo m)
    {
        Accessibility modifiers = Accessibility.NONE;
        switch (m.MemberType)
        {
            case MemberTypes.Field:
            {
                FieldInfo f = (FieldInfo)m;
                if (f.IsPrivate)
                    modifiers |= Accessibility.PRIVATE;
                if (f.IsPublic)
                    modifiers |= Accessibility.PUBLIC;
                if (f.IsStatic)
                    modifiers |= Accessibility.STATIC;
                if (!f.IsPublic && !f.IsPrivate)
                    modifiers |= Accessibility.PROTECTED;
                break;
            }
            case MemberTypes.Method:
            {
                MethodInfo mi = (MethodInfo)m;
                if (mi.IsPrivate)
                    modifiers |= Accessibility.PRIVATE;
                if (mi.IsPublic)
                    modifiers |= Accessibility.PUBLIC;
                if (mi.IsStatic)
                    modifiers |= Accessibility.STATIC;
                if (!mi.IsPublic && !mi.IsPrivate)
                    modifiers |= Accessibility.PROTECTED;
                break;
            }
            case MemberTypes.Property:
            {
                PropertyInfo p = (PropertyInfo)m;
                foreach (MethodInfo mi in p.GetAccessors())
                {
                    modifiers |= (Accessibility)GetModifiers(mi);
                }
                break;
            }
            default:
            {
                modifiers = Accessibility.STATIC|Accessibility.PUBLIC;
                break;
            }
        }
        return (int) modifiers;
    }

    public static void Main(string[] arg)
    {
        if (arg.Length > 0)
        {
            string[] argv = arg[0].Split(new string[] {sep},  StringSplitOptions.None);
            foreach (string a in argv)
            {
                try
                {
                    Assembly.LoadFrom(a);
                }
                catch (Exception e)
                {
                    System.Console.Error.WriteLine("exception: " + e.Message);
                    System.Console.Error.WriteLine(e.StackTrace);
                }
            }
        }

        try
        {
            bool first = true;
            while (true)
            {
                try
                {
                    Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    if (!first)
                        // Just to indicate that there's no more output from the command and we're ready for new input
                        System.Console.WriteLine(sep);
                    first = false;
                    string command = System.Console.ReadLine();
                    System.Console.Error.WriteLine("got: " + command);
                    if (command == null)
                        break;
                    string[] args = Regex.Split(command, sep);

                    if (args[0] == "-quit")
                    {
                        return;
                    }
                    else if (args[0] == "-findclass")
                    {
                        ArrayList modules = new ArrayList();
                        string line = null;
                        try
                        {
                            while ((line = System.Console.ReadLine()) != null)
                            {
                                if (line == sep)
                                    break;
                                modules.Add(line);
                            }
                        }
                        catch (Exception)
                        {}
                        bool found = false;
                        foreach (String mod in modules)
                        {
                            try
                            {
                                string fullname = args[1];
                                if (mod.Length > 0)
                                {
                                    fullname = mod + "." + fullname;
                                }
                                System.Console.Error.WriteLine("Trying " + fullname);
                                Type t2 = Type.GetType(fullname);

                                if (t2 == null)
                                {
                                    foreach (Assembly ass in assemblies)
                                    {
                                        t2 = ass.GetType(fullname);
                                        if (t2 != null)
                                            break;
                                    }
                                }
                                if (t2 != null)
                                {
                                    string typename = t2.FullName;
                                    System.Console.WriteLine(typename);
                                    System.Console.Error.WriteLine(typename);
                                    found = true;
                                    break;
                                }
                            }
                            catch (Exception e)
                            {
                                System.Console.Error.WriteLine(e.Message);
                                System.Console.Error.WriteLine(e.StackTrace);
                            }
                        }
                        if (found)
                            continue;
                        // Probably a namespace then?
                        AppDomain MyDomain = AppDomain.CurrentDomain;
                        Assembly[] AssembliesLoaded = MyDomain.GetAssemblies();

                        foreach (Assembly asm in AssembliesLoaded)
                        {
                            foreach (Type t3 in asm.GetTypes())
                            {
                                if (t3.Namespace == args[1])
                                {
                                    System.Console.WriteLine(args[1]);
                                    System.Console.Error.WriteLine(args[1]);
                                    found = true;
                                    break;
                                }
                            }
                            if (found)
                                break;
                        }
                        continue;
                    }
                    if (args.Length < 2)
                        continue;
                        int len = args.Length - 3;
                    if (len < 0)
                        len = 0;
                    string[] templateParam = new string[len];
                    for (int i = 0; i < len; i++)
                    {
                        templateParam[i] = args[i+3];
                    }
                    Type t = null;
                    try
                    {
                        t = GetType(assemblies, args[1], templateParam);
                    }
                    catch (Exception e)
                    {
                        System.Console.Error.WriteLine("exception: " + e.Message);
                        System.Console.Error.WriteLine(e.StackTrace);
                    }

                    if (args[0] == "-complete")
                    {
                        if (t != null)
                        {
                            foreach (MemberInfo m in t.GetMembers(BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.FlattenHierarchy))
                            {
                                switch (m.MemberType)
                                {
                                    case MemberTypes.Event:
                                    case MemberTypes.Field:
                                    case MemberTypes.Method:
                                    case MemberTypes.Property:
                                    {
                                        string completion = m.ToString();
                                        int index = completion.IndexOf(' ');
                                        string returnType = completion.Substring(0, index);
                                        completion = completion.Substring(index+1);

                                        string display = "";
                                        index = completion.IndexOf('(');
                                        int index2 = completion.LastIndexOf(')');
                                        if (index != -1 && index2 != -1)
                                        {
                                            string param = completion.Substring(index+1, index2-index-1);
                                            completion = completion.Substring(0, index+1);
                                            display = completion;
                                            string[] par = param.Split(new Char[]{','});
                                            int i = 1;
                                            foreach (string p in par)
                                            {
                                                string toadd = FixName(p.Trim());
                                                if (toadd.Length > 0)
                                                {
                                                    if (i > 1)
                                                    {
                                                        completion += ", ";
                                                        display += ", ";
                                                    }
                                                    display += toadd;
                                                    completion += "${" + i + ":" + toadd + "}";
                                                    i++;
                                                }
                                            }
                                            completion += ")";
                                            display += ")";
                                        }
                                        else
                                        {
                                            display = completion;
                                        }
                                        string insertion = completion;
                                        display += "\t" + FixName(returnType);

                                        System.Console.WriteLine(display + sep + insertion + sep + GetModifiers(m));
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            AppDomain MyDomain = AppDomain.CurrentDomain;
                            Assembly[] AssembliesLoaded = MyDomain.GetAssemblies();

                            foreach (Assembly asm in AssembliesLoaded)
                            {
                                foreach (Type t3 in asm.GetTypes())
                                {
                                    if (t3.Namespace == args[1])
                                    {
                                        System.Console.WriteLine(FixName(t3.Name) + "\tclass" + sep + FixName(t3.Name, true));
                                    }
                                }
                            }
                        }
                    }
                    else if (args[0] == "-returntype")
                    {
                        if (t != null)
                        {
                            bool found = false;
                            // This isn't 100% correct, but an instance where two things
                            // are named the same but return two different types would
                            // be considered rare.
                            foreach (MethodInfo m in t.GetMethods())
                            {
                                if (m.Name == args[2])
                                {
                                    System.Console.WriteLine(FixName(m.ReturnType.FullName));
                                    found = true;
                                    break;
                                }
                            }
                            if (found)
                                continue;
                            foreach (FieldInfo f in t.GetFields())
                            {
                                if (f.Name == args[2])
                                {
                                    System.Console.WriteLine(FixName(f.FieldType.FullName));
                                    found = true;
                                    break;
                                }
                            }
                            if (found)
                                continue;
                            foreach (EventInfo e in t.GetEvents())
                            {
                                if (e.Name == args[2])
                                {
                                    System.Console.WriteLine(FixName(e.EventHandlerType.FullName));
                                    found = true;
                                    break;
                                }
                            }
                            if (found)
                                continue;
                            foreach (PropertyInfo p in t.GetProperties())
                            {
                                if (p.Name == args[2])
                                {
                                    System.Console.WriteLine(FixName(p.PropertyType.FullName));
                                    found = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            bool found = false;;

                            foreach (Assembly asm in assemblies)
                            {
                                foreach (Type t3 in asm.GetTypes())
                                {
                                    if (t3.Namespace == args[1] && t3.Name == args[2])
                                    {
                                        System.Console.WriteLine(FixName(t3.FullName));
                                        found = true;
                                        break;
                                    }
                                }
                                if (found)
                                    break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    System.Console.Error.WriteLine(e);
                }
            }
        }
        catch (Exception e)
        {
            System.Console.Error.WriteLine(e);
        }
    }
}


using System;
using System.Reflection;
using System.Collections;

public class CompleteSharp
{
    private static string sep = ";;--;;";
    public static void Main(string[] arg)
    {
        ArrayList assemblies = new ArrayList();
        if (arg.Length > 0)
        {
            string[] argv = arg[0].Split(new string[] {sep},  StringSplitOptions.None);
            foreach (string a in argv)
            {
                try
                {
                    assemblies.Add(Assembly.LoadFrom(a));
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
                    if (!first)
                        // Just to indicate that there's no more output from the command and we're ready for new input
                        System.Console.WriteLine(sep);
                    first = false;
                    string command = System.Console.ReadLine();
                    System.Console.Error.WriteLine("got: " + command);
                    if (command == null)
                        break;
                    string[] args = command.Split(new Char[] {' '});
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
                                string fullname = mod + "." + args[1];
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
                                    System.Console.WriteLine(t2.FullName);
                                    System.Console.Error.WriteLine(t2.FullName);
                                    found = true;
                                    break;
                                }
                            }
                            catch (Exception)
                            {
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
                    Type t = null;
                    try
                    {
                        t = Type.GetType(args[1]);
                        if (t == null)
                        {
                            foreach (Assembly ass in assemblies)
                            {
                                t = ass.GetType(args[1]);
                                if (t != null)
                                    break;
                            }
                        }
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
                            foreach (MemberInfo m in t.GetMembers())
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

                                        string display = completion + "\t" + returnType;
                                        index = completion.IndexOf('(');
                                        int index2 = completion.LastIndexOf(')');
                                        if (index != -1 && index2 != -1)
                                        {
                                            string param = completion.Substring(index+1, index2-index-1);
                                            completion = completion.Substring(0, index+1);
                                            string[] par = param.Split(new Char[]{','});
                                            int i = 1;
                                            foreach (string p in par)
                                            {
                                                if (i > 1)
                                                    completion += ", ";
                                                completion += "${" + i + ":" + p.Trim() + "}";
                                                i++;
                                            }
                                            completion += ")";
                                        }
                                        string insertion = completion;

                                        System.Console.WriteLine(display + sep + insertion);
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
                                        System.Console.WriteLine(t3.Name + "\tclass" + sep + t3.Name);
                                    }
                                }
                            }
                        }
                    }
                    else if (args[0] == "-returntype")
                    {
                        if (t != null)
                        {
                            foreach (MemberInfo m in t.GetMembers())
                            {
                                if (m.Name == args[2])
                                {
                                    switch (m.MemberType)
                                    {
                                        case MemberTypes.Method:
                                        {
                                            MethodInfo i = t.GetMethod(m.Name);
                                            if (i != null)
                                                System.Console.WriteLine(i.ReturnType.FullName);
                                            break;
                                        }
                                        case MemberTypes.Field:
                                        {
                                            FieldInfo f = t.GetField(m.Name);
                                            if (f != null)
                                                System.Console.WriteLine(f.FieldType.FullName);
                                            break;
                                        }
                                        case MemberTypes.Event:
                                        {
                                            EventInfo e = t.GetEvent(m.Name);
                                            if (e != null)
                                                System.Console.WriteLine(e.EventHandlerType.FullName);
                                            break;
                                        }
                                        case MemberTypes.Property:
                                        {
                                            PropertyInfo p = t.GetProperty(m.Name);
                                            if (p != null)
                                                System.Console.WriteLine(p.PropertyType.FullName);
                                            break;
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                        else
                        {
                            AppDomain MyDomain = AppDomain.CurrentDomain;
                            Assembly[] AssembliesLoaded = MyDomain.GetAssemblies();
                            bool found = false;;

                            foreach (Assembly asm in AssembliesLoaded)
                            {
                                foreach (Type t3 in asm.GetTypes())
                                {
                                    if (t3.Namespace == args[1] && t3.Name == args[2])
                                    {
                                        System.Console.WriteLine(t3.FullName);
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


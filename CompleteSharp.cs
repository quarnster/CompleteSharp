using System;
using System.Reflection;
using System.Collections;

public class CompleteSharp
{
    private static string sep = ";;--;;";
    public static void Main()
    {
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
                                Type t2 = Type.GetType(mod + "." + args[1]);
                                System.Console.WriteLine(t2.FullName);
                                System.Console.Error.WriteLine(t2.FullName);
                                found = true;
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
                    }
                    catch (Exception)
                    {}

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
                                    string completion = m.ToString();
                                    int index = completion.IndexOf(' ');
                                    string returnType = completion.Substring(0, index);
                                    System.Console.WriteLine(returnType);
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


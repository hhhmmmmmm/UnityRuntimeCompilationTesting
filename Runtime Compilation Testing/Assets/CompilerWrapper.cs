// inspiration: 
// https://gist.github.com/SeargeDP/967f007ac896accfc214
// http://blog.davidebbo.com/2012/02/quick-fun-with-monos-csharp-compiler-as.html
// https://www.reddit.com/r/gamedev/comments/2zvlm1/sandbox_solution_for_c_scripts_using_monocsharp/
// http://www.amazedsaint.com/2010/09/c-as-scripting-language-in-your-net.html


using Mono.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

public class CompilerWrapper
{
    private Evaluator _evaluator;
    private CompilerContext _context;
    private StringBuilder _report;

    public int ErrorsCount { get { return _context.Report.Printer.ErrorsCount; } }
    public int WarningsCount { get { return _context.Report.Printer.WarningsCount; } }
    public string GetReport() { return _report.ToString(); }

    public CompilerWrapper()
    {
        // create new settings that will *not* load up all of standard lib by default
        // see: https://github.com/mono/mono/blob/master/mcs/mcs/settings.cs

        CompilerSettings settings = new CompilerSettings { LoadDefaultReferences = false, StdLib = false };
        this._report = new StringBuilder();
        this._context = new CompilerContext(settings, new StreamReportPrinter(new StringWriter(_report)));

        this._evaluator = new Evaluator(_context);
        this._evaluator.ReferenceAssembly(Assembly.GetExecutingAssembly());

        ImportAllowedTypes(BuiltInTypes, AdditionalTypes, QuestionableTypes);
    }

    /// <summary> Loads user code. Returns true on successful evaluation, or false on errors. </summary>
    public bool Execute(string path)
    {
        _report.Length = 0;
        var code = File.ReadAllText(path);
        return _evaluator.Run(code);
    }

    /// <summary> Creates new instances of types that are children of the specified type. </summary>
    public IEnumerable<T> CreateInstancesOf<T>()
    {
        var parent = typeof(T);
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var types = assemblies.SelectMany(assembly => {
            return assembly.GetTypes().Where(type => {
                return !(type.IsAbstract || type.IsInterface) && parent.IsAssignableFrom(type);
            });
        });
        return types.Select(type => (T)Activator.CreateInstance(type));
    }

    private void ImportAllowedTypes(params Type[][] allowedTypeArrays)
    {
        // expose Evaluator.importer and Evaluator.module
        var evtype = typeof(Evaluator);
        var importer = (ReflectionImporter)evtype
            .GetField("importer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_evaluator);
        var module = (ModuleContainer)evtype
            .GetField("module", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_evaluator);

        // expose MetadataImporter.ImportTypes(Type[], RootNamespace, bool)
        var importTypes = importer.GetType().GetMethod(
            "ImportTypes", BindingFlags.Instance | BindingFlags.Public, null, CallingConventions.Any,       // NOTE: had to change to Public from NonPublic
            new Type[] { typeof(Type[]), typeof(Namespace), typeof(bool) }, null);

        foreach (Type[] types in allowedTypeArrays)
        {
            importTypes.Invoke(importer, new object[] { types, module.GlobalRootNamespace, false });
        }
    }

    #region Allowed Types

    /// <summary>
    /// Basic built-in system types.
    /// </summary>
    private static Type[] BuiltInTypes = new Type[] {
        typeof(void),
        typeof(System.Type),
        typeof(System.Object),
        typeof(System.ValueType),
        typeof(System.Array),

        typeof(System.SByte),
        typeof(System.Byte),
        typeof(System.Int16),
        typeof(System.UInt16),
        typeof(System.Int32),
        typeof(System.UInt32),
        typeof(System.Int64),
        typeof(System.UInt64),
        typeof(System.Single),
        typeof(System.Double),
        typeof(System.Char),
        typeof(System.String),
        typeof(System.Boolean),
        typeof(System.Decimal),
        typeof(System.IntPtr),
        typeof(System.UIntPtr),
        typeof(System.Enum),
        typeof(System.Attribute),
        typeof(System.Delegate),
        typeof(System.MulticastDelegate),
        typeof(System.IDisposable),
        typeof(System.Exception),
        typeof(System.RuntimeFieldHandle),
        typeof(System.RuntimeTypeHandle),
        typeof(System.ParamArrayAttribute),
        typeof(System.Runtime.InteropServices.OutAttribute),
    };

    /// <summary>
    /// These types may be useful in scripts but they're not strictly necessary and 
    /// should be edited as desired.
    /// </summary>
    private static Type[] AdditionalTypes = new Type[] {

        // mscorlib System

        typeof(System.Action),
        typeof(System.Action<>),
        typeof(System.Action<,>),
        typeof(System.Action<,,>),
        typeof(System.Action<,,,>),
        typeof(System.ArgumentException),
        typeof(System.ArgumentNullException),
        typeof(System.ArgumentOutOfRangeException),
        typeof(System.ArithmeticException),
        typeof(System.ArraySegment<>),
        typeof(System.ArrayTypeMismatchException),
        typeof(System.Comparison<>),
        typeof(System.Convert),
        typeof(System.Converter<,>),
        typeof(System.DivideByZeroException),
        typeof(System.FlagsAttribute),
        typeof(System.FormatException),
        typeof(System.Func<>),
        typeof(System.Func<,>),
        typeof(System.Func<,,>),
        typeof(System.Func<,,,>),
        typeof(System.Func<,,,,>),
        typeof(System.Guid),
        typeof(System.IAsyncResult),
        typeof(System.ICloneable),
        typeof(System.IComparable),
        typeof(System.IComparable<>),
        typeof(System.IConvertible),
        typeof(System.ICustomFormatter),
        typeof(System.IEquatable<>),
        typeof(System.IFormatProvider),
        typeof(System.IFormattable),
        typeof(System.IndexOutOfRangeException),
        typeof(System.InvalidCastException),
        typeof(System.InvalidOperationException),
        typeof(System.InvalidTimeZoneException),
        typeof(System.Math),
        typeof(System.MidpointRounding),
        typeof(System.NonSerializedAttribute),
        typeof(System.NotFiniteNumberException),
        typeof(System.NotImplementedException),
        typeof(System.NotSupportedException),
        typeof(System.Nullable),
        typeof(System.Nullable<>),
        typeof(System.NullReferenceException),
        typeof(System.ObjectDisposedException),
        typeof(System.ObsoleteAttribute),
        typeof(System.OverflowException),
        typeof(System.Predicate<>),
        typeof(System.Random),
        typeof(System.RankException),
        typeof(System.SerializableAttribute),
        typeof(System.StackOverflowException),
        typeof(System.StringComparer),
        typeof(System.StringComparison),
        typeof(System.StringSplitOptions),
        typeof(System.SystemException),
        typeof(System.TimeoutException),
        typeof(System.TypeCode),
        typeof(System.Version),
        typeof(System.WeakReference),
        
        // mscorlib System.Collections
        
        typeof(System.Collections.BitArray),
        typeof(System.Collections.ICollection),
        typeof(System.Collections.IComparer),
        typeof(System.Collections.IDictionary),
        typeof(System.Collections.IDictionaryEnumerator),
        typeof(System.Collections.IEqualityComparer),
        typeof(System.Collections.IList),

        // mscorlib System.Collections.Generic

        typeof(System.Collections.IEnumerator),
        typeof(System.Collections.IEnumerable),
        typeof(System.Collections.Generic.Comparer<>),
        typeof(System.Collections.Generic.Dictionary<,>),
        typeof(System.Collections.Generic.EqualityComparer<>),
        typeof(System.Collections.Generic.ICollection<>),
        typeof(System.Collections.Generic.IComparer<>),
        typeof(System.Collections.Generic.IDictionary<,>),
        typeof(System.Collections.Generic.IEnumerable<>),
        typeof(System.Collections.Generic.IEnumerator<>),
        typeof(System.Collections.Generic.IEqualityComparer<>),
        typeof(System.Collections.Generic.IList<>),
        typeof(System.Collections.Generic.KeyNotFoundException),
        typeof(System.Collections.Generic.KeyValuePair<,>),
        typeof(System.Collections.Generic.List<>),
        
        // mscorlib System.Collections.ObjectModel

        typeof(System.Collections.ObjectModel.Collection<>),
        typeof(System.Collections.ObjectModel.KeyedCollection<,>),
        typeof(System.Collections.ObjectModel.ReadOnlyCollection<>),

        // System System.Collections.Generic

        typeof(System.Collections.Generic.LinkedList<>),
        typeof(System.Collections.Generic.LinkedListNode<>),
        typeof(System.Collections.Generic.Queue<>),
        typeof(System.Collections.Generic.SortedDictionary<,>),
        typeof(System.Collections.Generic.SortedList<,>),
        typeof(System.Collections.Generic.Stack<>),

        // System System.Collections.Specialized

        typeof(System.Collections.Specialized.BitVector32),

        // System.Core System.Collections.Generic

        typeof(System.Collections.Generic.HashSet<>),

        // System.Core System.Linq

        typeof(System.Linq.Enumerable),
        typeof(System.Linq.IGrouping<,>),
        typeof(System.Linq.ILookup<,>),
        typeof(System.Linq.IOrderedEnumerable<>),
        typeof(System.Linq.IOrderedQueryable),
        typeof(System.Linq.IOrderedQueryable<>),
        typeof(System.Linq.IQueryable),
        typeof(System.Linq.IQueryable<>),
        typeof(System.Linq.IQueryProvider),
        typeof(System.Linq.Lookup<,>),
        typeof(System.Linq.Queryable),
        
        // UnityEngine
        typeof(UnityEngine.Random),
        typeof(UnityEngine.Debug),
    };

    /// <summary>
    /// These types probably shouldn't be exposed, because they allow filesystem access,
    /// or because they provide more advanced functionality that mods shouldn't depend on.
    /// Proceed with caution.
    /// </summary>
    private static Type[] QuestionableTypes = new Type[] {
        
        //// mscorlib System
        
        //typeof(System.AsyncCallback),
        //typeof(System.BitConverter),
        //typeof(System.Buffer),
        //typeof(System.DateTime),
        //typeof(System.DateTimeKind),
        //typeof(System.DateTimeOffset),
        //typeof(System.DayOfWeek),
        //typeof(System.EventArgs),
        //typeof(System.EventHandler),
        //typeof(System.EventHandler<>),
        //typeof(System.TimeSpan),
        //typeof(System.TimeZone),
        //typeof(System.TimeZoneInfo),
        //typeof(System.TimeZoneNotFoundException),

        //// mscorlib System.IO
        
        //typeof(System.IO.BinaryReader),
        //typeof(System.IO.BinaryWriter),
        //typeof(System.IO.BufferedStream),
        //typeof(System.IO.EndOfStreamException),
        //typeof(System.IO.FileAccess),
        //typeof(System.IO.FileMode),
        //typeof(System.IO.FileNotFoundException),
        //typeof(System.IO.IOException),
        //typeof(System.IO.MemoryStream),
        //typeof(System.IO.Path),
        //typeof(System.IO.PathTooLongException),
        //typeof(System.IO.SeekOrigin),
        //typeof(System.IO.Stream),
        //typeof(System.IO.StringReader),
        //typeof(System.IO.StringWriter),
        //typeof(System.IO.TextReader),
        //typeof(System.IO.TextWriter),

        //// mscorlib System.Text
        
        //typeof(System.Text.ASCIIEncoding),
        //typeof(System.Text.Decoder),
        //typeof(System.Text.Encoder),
        //typeof(System.Text.Encoding),
        //typeof(System.Text.EncodingInfo),
        //typeof(System.Text.StringBuilder),
        //typeof(System.Text.UnicodeEncoding),
        //typeof(System.Text.UTF32Encoding),
        //typeof(System.Text.UTF7Encoding),
        //typeof(System.Text.UTF8Encoding),

        //// mscorlib System.Globalization
        
        //typeof(System.Globalization.CharUnicodeInfo),
        //typeof(System.Globalization.CultureInfo),
        //typeof(System.Globalization.DateTimeFormatInfo),
        //typeof(System.Globalization.DateTimeStyles),
        //typeof(System.Globalization.NumberFormatInfo),
        //typeof(System.Globalization.NumberStyles),
        //typeof(System.Globalization.RegionInfo),
        //typeof(System.Globalization.StringInfo),
        //typeof(System.Globalization.TextElementEnumerator),
        //typeof(System.Globalization.TextInfo),
        //typeof(System.Globalization.UnicodeCategory),
       
        //// System System.IO.Compression
        
        //typeof(System.IO.Compression.CompressionMode),
        //typeof(System.IO.Compression.DeflateStream),
        //typeof(System.IO.Compression.GZipStream),
        
        //// System System.Text.RegularExpressions

        //typeof(System.Text.RegularExpressions.Capture),
        //typeof(System.Text.RegularExpressions.CaptureCollection),
        //typeof(System.Text.RegularExpressions.Group),
        //typeof(System.Text.RegularExpressions.GroupCollection),
        //typeof(System.Text.RegularExpressions.Match),
        //typeof(System.Text.RegularExpressions.MatchCollection),
        //typeof(System.Text.RegularExpressions.MatchEvaluator),
        //typeof(System.Text.RegularExpressions.Regex),
        //typeof(System.Text.RegularExpressions.RegexCompilationInfo),
        //typeof(System.Text.RegularExpressions.RegexOptions),

    };
    #endregion
}
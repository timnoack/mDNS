// TODO: can we GPL this?
//
// In order to convert some functionality to Visual C#, the Java Language Conversion Assistant
// creates "support classes" that duplicate the original functionality.  
//
// Support classes replicate the functionality of the original code, but in some cases they are 
// substantially different architecturally. Although every effort is made to preserve the 
// original architecture of the application in the converted project, the user should be aware that 
// the primary goal of these support classes is to replicate functionality, and that at times 
// the architecture of the resulting solution may differ somewhat.
//

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Windows.System.Threading;
using System.Text;
using Windows.Foundation;
using Windows.Networking.Sockets;
using System.Threading.Tasks;

namespace mDNS
{

    /// <summary>
    /// Contains conversion support elements such as classes, interfaces and static methods.
    /// </summary>
    // TODO: get rid of this class
    public class SupportClass
    {
        /// <summary>
        /// Receives a byte array and returns it transformed in an sbyte array
        /// </summary>
        /// <param name="byteArray">Byte array to process</param>
        /// <returns>The transformed array</returns>
        public static sbyte[] ToSByteArray(byte[] byteArray)
        {
            sbyte[] sbyteArray = null;
            if (byteArray != null)
            {
                sbyteArray = new sbyte[byteArray.Length];
                for (int index = 0; index < byteArray.Length; index++)
                    sbyteArray[index] = (sbyte)byteArray[index];
            }
            return sbyteArray;
        }

        /*******************************/
        /// <summary>
        /// Provides functionality for classes that implements the IList interface.
        /// </summary>
        public class IListSupport
        {
            /// <summary>
            /// Ensures the capacity of the list to be greater or equal than the specified.
            /// </summary>
            /// <param name="list">The list to be checked.</param>
            /// <param name="capacity">The expected capacity.</param>
            public static void EnsureCapacity(ArrayList list, int capacity)
            {
                if (list.Capacity < capacity) list.Capacity = 2 * list.Capacity;
                if (list.Capacity < capacity) list.Capacity = capacity;
            }

            /// <summary>
            /// Adds all the elements contained into the specified collection, starting at the specified position.
            /// </summary>
            /// <param name="index">Position at which to add the first element from the specified collection.</param>
            /// <param name="list">The list used to extract the elements that will be added.</param>
            /// <returns>Returns true if all the elements were successfuly added. Otherwise returns false.</returns>
            public static bool AddAll(IList list, int index, ICollection c)
            {
                bool result = false;
                if (c != null)
                {
                    IEnumerator tempEnumerator = new ArrayList(c).GetEnumerator();
                    int tempIndex = index;

                    while (tempEnumerator.MoveNext())
                    {
                        list.Insert(tempIndex++, tempEnumerator.Current);
                        result = true;
                    }
                }

                return result;
            }

            /// <summary>
            /// Returns an enumerator of the collection starting at the specified position.
            /// </summary>
            /// <param name="index">The position to set the iterator.</param>
            /// <returns>An IEnumerator at the specified position.</returns>
            public static IEnumerator GetEnumerator(IList list, int index)
            {
                if ((index < 0) || (index > list.Count))
                    throw new IndexOutOfRangeException();

                IEnumerator tempEnumerator = list.GetEnumerator();
                if (index > 0)
                {
                    int i = 0;
                    while ((tempEnumerator.MoveNext()) && (i < index - 1))
                        i++;
                }
                return tempEnumerator;
            }
        }


        /*******************************/
        /// <summary>
        /// This class provides functionality not found in .NET collection-related interfaces.
        /// </summary>
        public class ICollectionSupport
        {
            /// <summary>
            /// Adds a new element to the specified collection.
            /// </summary>
            /// <param name="c">Collection where the new element will be added.</param>
            /// <param name="obj">Object to add.</param>
            /// <returns>true</returns>
            public static bool Add(ICollection c, object obj)
            {
                bool added = false;
                //Reflection. Invoke either the "add" or "Add" method.
                MethodInfo method;
                try
                {
                    //Get the "add" method for proprietary classes
                    method = c.GetType().GetMethod("Add");
                    if (method == null)
                        method = c.GetType().GetMethod("add");
                    int index = (int)method.Invoke(c, new object[] { obj });
                    if (index >= 0)
                        added = true;
                }
                catch (Exception e)
                {
                    throw e;
                }
                return added;
            }

            /// <summary>
            /// Adds all of the elements of the "c" collection to the "target" collection.
            /// </summary>
            /// <param name="target">Collection where the new elements will be added.</param>
            /// <param name="c">Collection whose elements will be added.</param>
            /// <returns>Returns true if at least one element was added, false otherwise.</returns>
            public static bool AddAll(ICollection target, ICollection c)
            {
                IEnumerator e = new ArrayList(c).GetEnumerator();
                bool added = false;

                //Reflection. Invoke "addAll" method for proprietary classes
                MethodInfo method;
                try
                {
                    method = target.GetType().GetMethod("addAll");

                    if (method != null)
                        added = (bool)method.Invoke(target, new object[] { c });
                    else
                    {
                        method = target.GetType().GetMethod("Add");
                        while (e.MoveNext() == true)
                        {
                            bool tempBAdded = (int)method.Invoke(target, new object[] { e.Current }) >= 0;
                            added = added ? added : tempBAdded;
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                return added;
            }

            /// <summary>
            /// Removes all the elements from the collection.
            /// </summary>
            /// <param name="c">The collection to remove elements.</param>
            public static void Clear(ICollection c)
            {
                //Reflection. Invoke "Clear" method or "clear" method for proprietary classes
                MethodInfo method;
                try
                {
                    method = c.GetType().GetMethod("Clear");

                    if (method == null)
                        method = c.GetType().GetMethod("clear");

                    method.Invoke(c, new object[] { });
                }
                catch (Exception e)
                {
                    throw e;
                }
            }

            /// <summary>
            /// Determines whether the collection contains the specified element.
            /// </summary>
            /// <param name="c">The collection to check.</param>
            /// <param name="obj">The object to locate in the collection.</param>
            /// <returns>true if the element is in the collection.</returns>
            public static bool Contains(ICollection c, object obj)
            {
                bool contains = false;

                //Reflection. Invoke "contains" method for proprietary classes
                MethodInfo method;
                try
                {
                    method = c.GetType().GetMethod("Contains");

                    if (method == null)
                        method = c.GetType().GetMethod("contains");

                    contains = (bool)method.Invoke(c, new object[] { obj });
                }
                catch (Exception e)
                {
                    throw e;
                }

                return contains;
            }

            /// <summary>
            /// Determines whether the collection contains all the elements in the specified collection.
            /// </summary>
            /// <param name="target">The collection to check.</param>
            /// <param name="c">Collection whose elements would be checked for containment.</param>
            /// <returns>true id the target collection contains all the elements of the specified collection.</returns>
            public static bool ContainsAll(ICollection target, ICollection c)
            {
                IEnumerator e = c.GetEnumerator();

                bool contains = false;

                //Reflection. Invoke "containsAll" method for proprietary classes or "Contains" method for each element in the collection
                MethodInfo method;
                try
                {
                    method = target.GetType().GetMethod("containsAll");

                    if (method != null)
                        contains = (bool)method.Invoke(target, new Object[] { c });
                    else
                    {
                        method = target.GetType().GetMethod("Contains");
                        while (e.MoveNext() == true)
                        {
                            if ((contains = (bool)method.Invoke(target, new Object[] { e.Current })) == false)
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }

                return contains;
            }

            /// <summary>
            /// Removes the specified element from the collection.
            /// </summary>
            /// <param name="c">The collection where the element will be removed.</param>
            /// <param name="obj">The element to remove from the collection.</param>
            public static bool Remove(ICollection c, object obj)
            {
                bool changed = false;

                //Reflection. Invoke "remove" method for proprietary classes or "Remove" method
                MethodInfo method;
                try
                {
                    method = c.GetType().GetMethod("remove");

                    if (method != null)
                        method.Invoke(c, new object[] { obj });
                    else
                    {
                        method = c.GetType().GetMethod("Contains");
                        changed = (bool)method.Invoke(c, new object[] { obj });
                        method = c.GetType().GetMethod("Remove");
                        method.Invoke(c, new object[] { obj });
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }

                return changed;
            }

            /// <summary>
            /// Removes all the elements from the specified collection that are contained in the target collection.
            /// </summary>
            /// <param name="target">Collection where the elements will be removed.</param>
            /// <param name="c">Elements to remove from the target collection.</param>
            /// <returns>true</returns>
            public static bool RemoveAll(ICollection target, ICollection c)
            {
                ArrayList al = ToArrayList(c);
                IEnumerator e = al.GetEnumerator();

                //Reflection. Invoke "removeAll" method for proprietary classes or "Remove" for each element in the collection
                MethodInfo method;
                try
                {
                    method = target.GetType().GetMethod("removeAll");

                    if (method != null)
                        method.Invoke(target, new object[] { al });
                    else
                    {
                        method = target.GetType().GetMethod("Remove");
                        MethodInfo methodContains = target.GetType().GetMethod("Contains");

                        while (e.MoveNext() == true)
                        {
                            while ((bool)methodContains.Invoke(target, new object[] { e.Current }) == true)
                                method.Invoke(target, new object[] { e.Current });
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                return true;
            }

            /// <summary>
            /// Retains the elements in the target collection that are contained in the specified collection
            /// </summary>
            /// <param name="target">Collection where the elements will be removed.</param>
            /// <param name="c">Elements to be retained in the target collection.</param>
            /// <returns>true</returns>
            public static bool RetainAll(ICollection target, ICollection c)
            {
                IEnumerator e = new ArrayList(target).GetEnumerator();
                ArrayList al = new ArrayList(c);

                //Reflection. Invoke "retainAll" method for proprietary classes or "Remove" for each element in the collection
                MethodInfo method;
                try
                {
                    method = c.GetType().GetMethod("retainAll");

                    if (method != null)
                        method.Invoke(target, new object[] { c });
                    else
                    {
                        method = c.GetType().GetMethod("Remove");

                        while (e.MoveNext() == true)
                        {
                            if (al.Contains(e.Current) == false)
                                method.Invoke(target, new object[] { e.Current });
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }

                return true;
            }

            /// <summary>
            /// Returns an array containing all the elements of the collection.
            /// </summary>
            /// <returns>The array containing all the elements of the collection.</returns>
            public static object[] ToArray(ICollection c)
            {
                int index = 0;
                object[] objects = new object[c.Count];
                IEnumerator e = c.GetEnumerator();

                while (e.MoveNext())
                    objects[index++] = e.Current;

                return objects;
            }

            /// <summary>
            /// Obtains an array containing all the elements of the collection.
            /// </summary>
            /// <param name="objects">The array into which the elements of the collection will be stored.</param>
            /// <returns>The array containing all the elements of the collection.</returns>
            public static object[] ToArray(ICollection c, object[] objects)
            {
                int index = 0;

                Type type = objects.GetType().GetElementType();
                object[] objs = (object[])Array.CreateInstance(type, c.Count);

                IEnumerator e = c.GetEnumerator();

                while (e.MoveNext())
                    objs[index++] = e.Current;

                //If objects is smaller than c then do not return the new array in the parameter
                if (objects.Length >= c.Count)
                    objs.CopyTo(objects, 0);

                return objs;
            }

            /// <summary>
            /// Converts an ICollection instance to an ArrayList instance.
            /// </summary>
            /// <param name="c">The ICollection instance to be converted.</param>
            /// <returns>An ArrayList instance in which its elements are the elements of the ICollection instance.</returns>
            public static ArrayList ToArrayList(ICollection c)
            {
                ArrayList tempArrayList = new ArrayList();
                IEnumerator tempEnumerator = c.GetEnumerator();
                while (tempEnumerator.MoveNext())
                    tempArrayList.Add(tempEnumerator.Current);
                return tempArrayList;
            }
        }


        /*******************************/
        /// <summary>
        /// Summary description for EqualsSupport.
        /// </summary>
        public class EqualsSupport
        {
            /// <summary>
            /// Determines whether two Collections instances are equal.
            /// </summary>
            /// <param name="source">The first Collections to compare. </param>
            /// <param name="target">The second Collections to compare. </param>
            /// <returns>Return true if the first collection is the same instance as the second collection, otherwise returns false.</returns>
            public static bool Equals(ICollection source, ICollection target)
            {
                bool equal = true;

                ArrayList sourceInterfaces = new ArrayList(source.GetType().GetInterfaces());
                ArrayList targetInterfaces = new ArrayList(target.GetType().GetInterfaces());

                if (sourceInterfaces.Contains(Type.GetType("SupportClass+SetSupport")) &&
                    !targetInterfaces.Contains(Type.GetType("SupportClass+SetSupport")))
                    equal = false;
                else if (targetInterfaces.Contains(Type.GetType("SupportClass+SetSupport")) &&
                    !sourceInterfaces.Contains(Type.GetType("SupportClass+SetSupport")))
                    equal = false;

                if (equal)
                {
                    IEnumerator sourceEnumerator = ReverseStack(source);
                    IEnumerator targetEnumerator = ReverseStack(target);

                    if (source.Count != target.Count)
                        equal = false;

                    while (sourceEnumerator.MoveNext() && targetEnumerator.MoveNext())
                        if (!sourceEnumerator.Current.Equals(targetEnumerator.Current))
                            equal = false;
                }

                return equal;
            }

            /// <summary>
            /// Determines if a Collection is equal to the Object.
            /// </summary>
            /// <param name="source">The first Collections to compare.</param>
            /// <param name="target">The Object to compare.</param>
            /// <returns>Return true if the first collection contains the same values of the second Object, otherwise returns false.</returns>
            public static bool Equals(ICollection source, object target)
            {
                return (target is ICollection) ? Equals(source, (ICollection)target) : false;
            }

            /// <summary>
            /// Determines if a IDictionaryEnumerator is equal to the Object.
            /// </summary>
            /// <param name="source">The first IDictionaryEnumerator to compare.</param>
            /// <param name="target">The second Object to compare.</param>
            /// <returns>Return true if the first IDictionaryEnumerator contains the same values of the second Object, otherwise returns false.</returns>
            public static bool Equals(IDictionaryEnumerator source, object target)
            {
                return (target is IDictionaryEnumerator) ? Equals(source, (IDictionaryEnumerator)target) : false;
            }

            /// <summary>
            /// Determines if a IDictionary is equal to the Object.
            /// </summary>
            /// <param name="source">The first IDictionary to compare.</param>
            /// <param name="target">The second Object to compare.</param>
            /// <returns>Return true if the first IDictionary contains the same values of the second Object, otherwise returns false.</returns>
            public static bool Equals(IDictionary source, object target)
            {
                return (target is IDictionary) ? Equals(source, (IDictionary)target) : false;
            }

            /// <summary>
            /// Determines whether two IDictionaryEnumerator instances are equals.
            /// </summary>
            /// <param name="source">The first IDictionaryEnumerator to compare.</param>
            /// <param name="target">The second IDictionaryEnumerator to compare.</param>
            /// <returns>Return true if the first IDictionaryEnumerator contains the same values as the second IDictionaryEnumerator, otherwise return false.</returns>
            public static bool Equals(IDictionaryEnumerator source, IDictionaryEnumerator target)
            {
                while (source.MoveNext() && target.MoveNext())
                    if (source.Key.Equals(target.Key))
                        if (source.Value.Equals(target.Value))
                            return true;
                return false;
            }

            /// <summary>
            /// Reverses the Stack Collection received.
            /// </summary>
            /// <param name="collection">The collection to reverse.</param>
            /// <returns>The collection received in reverse order if it was a Stack type, otherwise it does 
            /// nothing to the collection.</returns>
            public static System.Collections.IEnumerator ReverseStack(ICollection collection)
            {
                if ((collection.GetType()) == (typeof(Stack)))
                {
                    ArrayList collectionStack = new ArrayList(collection);
                    collectionStack.Reverse();
                    return collectionStack.GetEnumerator();
                }
                else
                    return collection.GetEnumerator();
            }

            /// <summary>
            /// Determines whether two IDictionary instances are equal.
            /// </summary>
            /// <param name="source">The first Collection to compare.</param>
            /// <param name="target">The second Collection to compare.</param>
            /// <returns>Return true if the first collection is the same instance as the second collection, otherwise return false.</returns>
            public static bool Equals(IDictionary source, IDictionary target)
            {
                Hashtable targetAux = new Hashtable(target);

                if (source.Count == targetAux.Count)
                {
                    IEnumerator sourceEnum = source.Keys.GetEnumerator();
                    while (sourceEnum.MoveNext())
                        if (targetAux.Contains(sourceEnum.Current))
                            targetAux.Remove(sourceEnum.Current);
                        else
                            return false;
                }
                else
                    return false;
                if (targetAux.Count == 0)
                    return true;
                else
                    return false;
            }
        }


        /*******************************/
        /// <summary>
        /// Class to manage packets
        /// </summary>
        public class PacketSupport
        {
            private byte[] data;
            private int length;
            private IPEndPoint ipEndPoint;

            int port = -1;
            IPAddress address = null;

            /// <summary>
            /// Constructor for the packet
            /// </summary>	
            /// <param name="data">The buffer to store the data</param>	
            /// <param name="data">The length of the data sent</param>	
            /// <returns>A new packet to receive data of the specified length</returns>	
            public PacketSupport(byte[] data, int length)
            {
                if (length > data.Length)
                    throw new ArgumentException("illegal length");

                this.data = data;
                this.length = length;
                this.ipEndPoint = null;
            }

            /// <summary>
            /// Constructor for the packet
            /// </summary>	
            /// <param name="data">The data to be sent</param>	
            /// <param name="data">The length of the data to be sent</param>	
            /// <param name="data">The IP of the destination point</param>	
            /// <returns>A new packet with the data, length and ipEndPoint set</returns>
            public PacketSupport(byte[] data, int length, IPEndPoint ipendpoint)
            {
                if (length > data.Length)
                    throw new ArgumentException("illegal length");

                this.data = data;
                this.length = length;
                this.ipEndPoint = ipendpoint;
            }

            /// <summary>
            /// Gets and sets the address of the IP
            /// </summary>			
            /// <returns>The IP address</returns>
            public IPEndPoint IPEndPoint
            {
                get
                {
                    return this.ipEndPoint;
                }
                set
                {
                    this.ipEndPoint = value;
                }
            }

            /// <summary>
            /// Gets and sets the address
            /// </summary>			
            /// <returns>The int value of the address</returns>
            public IPAddress Address
            {
                get
                {
                    return address;
                }
                set
                {
                    address = value;
                    if (this.ipEndPoint == null)
                    {
                        if (Port >= 0 && Port <= 0xFFFF)
                            this.ipEndPoint = new IPEndPoint(value, Port);
                    }
                    else
                        this.ipEndPoint.Address = value;
                }
            }

            /// <summary>
            /// Gets and sets the port
            /// </summary>			
            /// <returns>The int value of the port</returns>
            public int Port
            {
                get
                {
                    return port;
                }
                set
                {
                    if (value < 0 || value > 0xFFFF)
                        throw new ArgumentException("Port out of range:" + value);

                    port = value;
                    if (this.ipEndPoint == null)
                    {
                        if (Address != null)
                            this.ipEndPoint = new IPEndPoint(Address, value);
                    }
                    else
                        this.ipEndPoint.Port = value;
                }
            }

            /// <summary>
            /// Gets and sets the length of the data
            /// </summary>			
            /// <returns>The int value of the length</returns>
            public int Length
            {
                get
                {
                    return this.length;
                }
                set
                {
                    if (value > data.Length)
                        throw new ArgumentException("illegal length");

                    this.length = value;
                }
            }

            /// <summary>
            /// Gets and sets the byte array that contains the data
            /// </summary>			
            /// <returns>The byte array that contains the data</returns>
            public byte[] Data
            {
                get
                {
                    return this.data;
                }

                set
                {
                    this.data = value;
                }
            }
        }
        /*******************************/
        /// <summary>
        /// Converts an array of sbytes to an array of bytes
        /// </summary>
        /// <param name="sbyteArray">The array of sbytes to be converted</param>
        /// <returns>The new array of bytes</returns>
        public static byte[] ToByteArray(sbyte[] sbyteArray)
        {
            byte[] byteArray = null;

            if (sbyteArray != null)
            {
                byteArray = new byte[sbyteArray.Length];
                for (int index = 0; index < sbyteArray.Length; index++)
                    byteArray[index] = (byte)sbyteArray[index];
            }
            return byteArray;
        }

        /// <summary>
        /// Converts a string to an array of bytes
        /// </summary>
        /// <param name="sourceString">The string to be converted</param>
        /// <returns>The new array of bytes</returns>
        public static byte[] ToByteArray(string sourceString)
        {
            return UTF8Encoding.UTF8.GetBytes(sourceString);
        }

        /// <summary>
        /// Converts a array of object-type instances to a byte-type array.
        /// </summary>
        /// <param name="tempObjectArray">Array to convert.</param>
        /// <returns>An array of byte type elements.</returns>
        public static byte[] ToByteArray(object[] tempObjectArray)
        {
            byte[] byteArray = null;
            if (tempObjectArray != null)
            {
                byteArray = new byte[tempObjectArray.Length];
                for (int index = 0; index < tempObjectArray.Length; index++)
                    byteArray[index] = (byte)tempObjectArray[index];
            }
            return byteArray;
        }

        /*******************************/
        /// <summary>
        /// Converts the specified collection to its string representation.
        /// </summary>
        /// <param name="c">The collection to convert to string.</param>
        /// <returns>A string representation of the specified collection.</returns>
        public static string CollectionToString(ICollection c)
        {
            StringBuilder s = new StringBuilder();

            if (c != null)
            {
                ArrayList l = new ArrayList(c);

                bool isDictionary = (c is BitArray ||
                                     c is Hashtable ||
                                     c is IDictionary ||
                                     c is NameValueCollection ||
                                     (l.Count > 0 && l[0] is DictionaryEntry));
                for (int index = 0; index < l.Count; index++)
                {
                    if (l[index] == null)
                        s.Append("null");
                    else if (!isDictionary)
                        s.Append(l[index]);
                    else
                    {
                        isDictionary = true;
                        if (c is NameValueCollection)
                            s.Append(((NameValueCollection)c).GetKey(index));
                        else
                            s.Append(((DictionaryEntry)l[index]).Key);
                        s.Append("=");
                        if (c is NameValueCollection)
                            s.Append(((NameValueCollection)c).GetValues(index)[0]);
                        else
                            s.Append(((DictionaryEntry)l[index]).Value);

                    }
                    if (index < l.Count - 1)
                        s.Append(", ");
                }

                if (isDictionary)
                {
                    if (c is ArrayList)
                        isDictionary = false;
                }
                if (isDictionary)
                {
                    s.Insert(0, "{");
                    s.Append("}");
                }
                else
                {
                    s.Insert(0, "[");
                    s.Append("]");
                }
            }
            else
                s.Insert(0, "null");
            return s.ToString();
        }

        /// <summary>
        /// Tests if the specified object is a collection and converts it to its string representation.
        /// </summary>
        /// <param name="obj">The object to convert to string</param>
        /// <returns>A string representation of the specified object.</returns>
        public static string CollectionToString(object obj)
        {
            string result = "";

            if (obj != null)
            {
                if (obj is ICollection)
                    result = CollectionToString((ICollection)obj);
                else
                    result = obj.ToString();
            }
            else
                result = "null";

            return result;
        }
        /*******************************/
        /// <summary>
        /// Converts an array of sbytes to an array of chars
        /// </summary>
        /// <param name="sByteArray">The array of sbytes to convert</param>
        /// <returns>The new array of chars</returns>
        public static char[] ToCharArray(sbyte[] sByteArray)
        {
            return UTF8Encoding.UTF8.GetChars(ToByteArray(sByteArray));
        }

        /// <summary>
        /// Converts an array of bytes to an array of chars
        /// </summary>
        /// <param name="byteArray">The array of bytes to convert</param>
        /// <returns>The new array of chars</returns>
        public static char[] ToCharArray(byte[] byteArray)
        {
            return UTF8Encoding.UTF8.GetChars(byteArray);
        }

        /*******************************/
        /// <summary>
        /// Support class used to handle threads
        /// </summary>
        public class ThreadClass
        {
            private IAsyncAction asyncAction;

            public WorkItemPriority Priority { get; set; }
            public WorkItemHandler WorkItem { get; private set; }

            public ThreadClass(WorkItemHandler workItem)
            {
                this.WorkItem = workItem;
                Priority = WorkItemPriority.Normal;
            }

            public void Start()
            {
                asyncAction = ThreadPool.RunAsync(new WorkItemHandler(WorkItem), Priority);
            }

            public async Task Join()
            {
                await asyncAction;
            }
        }


        /*******************************/
        /// <summary>
        /// Support class used to extend UdpClient class functionality
        /// </summary>
        public class UdpClientSupport : Socket
        {

            public int port = -1;

            public IPEndPoint ipEndPoint = null;

            public String host = null;


            /// <summary>
            /// Initializes a new instance of the UdpClientSupport class, and binds it to the local port number provided.
            /// </summary>
            /// <param name="port">The local port number from which you intend to communicate.</param>
            public UdpClientSupport(int port) : base(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                this.port = port;
            }

            /// <summary>
            /// Initializes a new instance of the UdpClientSupport class.
            /// </summary>
            public UdpClientSupport() : base(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
            }

            /// <summary>
            /// Initializes a new instance of the UdpClientSupport class,
            /// and binds it to the specified local endpoint.
            /// </summary>
            /// <param name="IP">An IPEndPoint that respresents the local endpoint to which you bind the UDP connection.</param>
            public UdpClientSupport(IPEndPoint IP) : base(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                this.ipEndPoint = IP;
                this.port = this.ipEndPoint.Port;
            }

            /// <summary>
            /// Initializes a new instance of the UdpClientSupport class,
            /// and and establishes a default remote host.
            /// </summary>
            /// <param name="host">The name of the remote DNS host to which you intend to connect.</param>
            /// <param name="port">The remote port number to which you intend to connect. </param>
            public UdpClientSupport(string host, int port) : base(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                this.host = host;
                this.port = port;
            }


            /// <summary>
            /// Sends a UDP datagram to the host at the specified remote endpoint.
            /// </summary>
            /// <param name="tempClient">Client to use as source for sending the datagram</param>
            /// <param name="packet">Packet containing the datagram data to send</param>
            public static async Task Send(UdpClient tempClient, PacketSupport packet)
            {
                await tempClient.Send(packet.Data, packet.Length, packet.IPEndPoint);
            }


            /// <summary>
            /// Gets and sets the address of the IP
            /// </summary>			
            /// <returns>The IP address</returns>
            public IPEndPoint IPEndPoint
            {
                get
                {
                    return this.ipEndPoint;
                }
                set
                {
                    this.ipEndPoint = value;
                }
            }

            /// <summary>
            /// Gets and sets the port
            /// </summary>			
            /// <returns>The int value of the port</returns>
            public int Port
            {
                get
                {
                    return this.port;
                }
                set
                {
                    if (value < 0 || value > 0xFFFF)
                        throw new ArgumentException("Port out of range:" + value);

                    this.port = value;
                }
            }


            /// <summary>
            /// Gets the address of the IP
            /// </summary>			
            /// <returns>The IP address</returns>
            public IPAddress getIPEndPointAddress()
            {
                if (this.ipEndPoint == null)
                    return null;
                else
                    return (this.ipEndPoint.Address == null) ? null : this.ipEndPoint.Address;
            }

        }

        /*******************************/
        /// <summary>
        /// SupportClass for the HashSet class.
        /// </summary>
        public class HashSetSupport : ArrayList, SetSupport
        {
            public HashSetSupport() : base()
            {
            }

            public HashSetSupport(ICollection c)
            {
                this.AddAll(c);
            }

            public HashSetSupport(int capacity) : base(capacity)
            {
            }

            /// <summary>
            /// Adds a new element to the ArrayList if it is not already present.
            /// </summary>		
            /// <param name="obj">Element to insert to the ArrayList.</param>
            /// <returns>Returns true if the new element was inserted, false otherwise.</returns>
            new public virtual bool Add(object obj)
            {
                bool inserted;

                if ((inserted = this.Contains(obj)) == false)
                {
                    base.Add(obj);
                }

                return !inserted;
            }

            /// <summary>
            /// Adds all the elements of the specified collection that are not present to the list.
            /// </summary>
            /// <param name="c">Collection where the new elements will be added</param>
            /// <returns>Returns true if at least one element was added, false otherwise.</returns>
            public bool AddAll(ICollection c)
            {
                IEnumerator e = new ArrayList(c).GetEnumerator();
                bool added = false;

                while (e.MoveNext() == true)
                {
                    if (this.Add(e.Current) == true)
                        added = true;
                }

                return added;
            }

            /// <summary>
            /// Returns a copy of the HashSet instance.
            /// </summary>		
            /// <returns>Returns a shallow copy of the current HashSet.</returns>
            public override object Clone()
            {
                return base.MemberwiseClone();
            }
        }


        /*******************************/
        /// <summary>
        /// Represents a collection ob objects that contains no duplicate elements.
        /// </summary>	
        public interface SetSupport : ICollection, IList
        {
            /// <summary>
            /// Adds a new element to the Collection if it is not already present.
            /// </summary>
            /// <param name="obj">The object to add to the collection.</param>
            /// <returns>Returns true if the object was added to the collection, otherwise false.</returns>
            new bool Add(object obj);

            /// <summary>
            /// Adds all the elements of the specified collection to the Set.
            /// </summary>
            /// <param name="c">Collection of objects to add.</param>
            /// <returns>true</returns>
            bool AddAll(ICollection c);
        }

    }
}
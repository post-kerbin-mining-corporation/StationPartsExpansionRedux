using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;

namespace HabUtils.KIS
{

  public class KISWrapper
  {
    static AssemblyLoader.LoadedAssembly KISasm;
    static Type KISAddonConfig_class;
    static MethodInfo kis_AddPodInventories;

    public delegate void AddPodInventoriesDelegate(Part part, int crewCapacity);
    public static AddPodInventoriesDelegate AddPodInventories = null;

    static bool initialized;

    public static bool Initialize()
    {
      if (!initialized)
      {
        KISasm = AssemblyLoader.loadedAssemblies.Where(a => a.assembly.GetName().Name.Equals("KIS", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
        if (KISasm == null)
        {
          return false;
        }
        KISAddonConfig_class = KISasm.assembly.GetTypes().Where(t => t.Name.Equals("KISAddonConfig")).FirstOrDefault();
        if (KISAddonConfig_class == null)
        {
          return false;
        }
        kis_AddPodInventories = KISAddonConfig_class.GetMethod("AddPodInventories", BindingFlags.Public | BindingFlags.Static);
        if (kis_AddPodInventories == null)
        {
          return false;
        }

        AddPodInventories = (AddPodInventoriesDelegate)Delegate.CreateDelegate(typeof(AddPodInventoriesDelegate), null, kis_AddPodInventories);
        initialized = true;
      }
      return AddPodInventories != null;
    }
  }
}
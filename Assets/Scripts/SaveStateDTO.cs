using System;
using System.Collections.Generic;

[Serializable]
public class SaveStateDTOWrapper
{
    public long id;
    public int slot;
    public string saveData;
}

[Serializable]
public class SaveStateDTOWrapperList
{
    public List<SaveStateDTOWrapper> states;
}

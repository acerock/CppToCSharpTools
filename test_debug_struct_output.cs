using Agresso.Types;
using Agresso.Interface.CoreServices;
using BatchNet;
using BatchNet.Compatibility;
using BatchNet.Fundamentals.Compatibility;
using U4.BatchNet.ServerLib.Compatibility;
using static BatchNet.Compatibility.Level1;
using static BatchNet.Compatibility.Level2;
using static BatchNet.Compatibility.BatchApi;

namespace U4.BatchNet.l10.Compatibility;

internal class StructOne
{
    protected agrint lTestType;

    //#region Just a h-file pragma test

        // att-id member comment
    public TAttId attId;
    public TDimValue dimVal;

    //#endregion // Comment test

        public StructOne(const TAttid& inAttId, const TDimValue &inDimVal, agrint lInTestType = 0)
    {
        lTestType = lInTestType;
        attid = inAttId;
        dimVal = inDimVal;
    }

    }

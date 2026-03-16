namespace Contracts.Infrastructure;

public class MarkeetingTransactionMessageFactory
{
     private static MarketingTransactionMessageResponse CreateFromInMsg(RawMessage inMsg)
     {
        var op = string.Empty;
        var comment = string.Empty;
        var profileAddr = string.Empty;
        
        var parentAddr = string.Empty;
        ulong pos = 0;
        var posSet = false;

        ulong queryId = 0;
        ulong key = 0;
        ulong m = 0;

        try
        {
            // cancel_task
            if (inMsg.OpCode.Equals("0x0BA25F1E9", StringComparison.InvariantCultureIgnoreCase)) // should be 0xba25f1e9
            {
                // cancel_task#ba25f1e9  query_id:uint64  key:uint32 = MultiInternalMsg;
                op = "cancel_task";

                var slice = inMsg.MsgData.Body.Parse();
                var opCode = slice.LoadUInt(32);
                queryId = (ulong)slice.LoadUInt(64);
                key = (ulong)slice.LoadUInt(32);
            }
            
            // reward
            else if (inMsg.OpCode.Equals("0x0D4C89207", StringComparison.InvariantCultureIgnoreCase)) // should be 0xd4c89207
            {
                // reward#d4c89207  query_id:uint64  m:Matrix  parent:MsgAddress  craeted_at:uint64  fill_count: (#<= 4)  profiles:^PlaceProfiles = MultiInternalMsg;
                op = "reward";

                var slice = inMsg.MsgData.Body.Parse();
                var opCode = slice.LoadUInt(32);
                queryId = (ulong)slice.LoadUInt(64);
                m = (ulong)slice.LoadUInt(3);
            }
            
            // buy_place
            else if (inMsg.OpCode.Equals("0x179b74a8", StringComparison.InvariantCultureIgnoreCase)) // should be 
            {
                // buy_place#179b74a8  query_id:uint64  m:Matrix  profile:Address  pos:(Maybe ^PlacePos) = MultiInternalMsg;
                op = "buy_place";
                
                var slice = inMsg.MsgData.Body.Parse();
                var opCode = slice.LoadUInt(32);
                queryId = (ulong)slice.LoadUInt(64);
                m = (ulong)slice.LoadUInt(3);
                var profile = slice.LoadAddress();
                
                profileAddr = profile?.ToString() ?? string.Empty;
                comment = $"m:{m}";
            }
            
            // lock_pos
            else if (inMsg.OpCode.Equals("0x6d31ad42", StringComparison.InvariantCultureIgnoreCase))
            {
                op = "lock_pos";
                
                // lock_pos#6d31ad42  query_id:uint64  m:Matrix  profile:Address  pos:^PlacePos = MultiInternalMsg;
                
                var slice = inMsg.MsgData.Body.Parse();
                var opCode = slice.LoadUInt(32);
                queryId = (ulong)slice.LoadUInt(64);
                m = (ulong)slice.LoadUInt(3);
                var profile = slice.LoadAddress();
                profileAddr = profile?.ToString() ?? string.Empty;

                var posSlice = slice.LoadRef().Parse();
                var parent = posSlice.LoadAddress();
                parentAddr = parent?.ToString() ?? string.Empty;

                if (posSlice.RemainderBits > 0)
                {
                    pos = (ulong)posSlice.LoadUInt(1);
                    posSet = true;
                }
                
                comment = $"m:{m}  parent:{parentAddr}  pos:{pos}";
            }
            
            // unlock_pos
            else if  (inMsg.OpCode.Equals("0x77d27591", StringComparison.InvariantCultureIgnoreCase))
            {
                op = "unlock_pos";
                
                // unlock_pos#77d27591  query_id:uint64  m:Matrix  profile:Address  pos:^PlacePos = MultiInternalMsg;
                
                var slice = inMsg.MsgData.Body.Parse();
                var opCode = slice.LoadUInt(32);
                queryId = (ulong)slice.LoadUInt(64);
                m = (ulong)slice.LoadUInt(3);
                var profile = slice.LoadAddress();
                profileAddr = profile?.ToString() ?? string.Empty;
                
                var posSlice = slice.LoadRef().Parse();
                var parent = posSlice.LoadAddress();
                parentAddr = parent?.ToString() ?? string.Empty;
                
                if (posSlice.RemainderBits > 0)
                {
                    pos = (ulong)posSlice.LoadUInt(1);
                    posSet = true;
                }
                
                comment = $"m:{m}  parent:{parentAddr}  pos:{pos}";
            }
            
            // deploy_place
            else if  (inMsg.OpCode.Equals("0x609ecd5a", StringComparison.InvariantCultureIgnoreCase))
            {
                op = "deploy_place";
                
                // deploy_place#609ecd5a  query_id:uint64  key:uint32  parent:MsgAddress  profiles:^PlaceProfiles = MultiInternalMsg;
                
                var slice = inMsg.MsgData.Body.Parse();
                var opCode = slice.LoadUInt(32);
                queryId = (ulong)slice.LoadUInt(64);
                key = (ulong)slice.LoadUInt(32);
                
                var parent = slice.LoadAddress();
                parentAddr = parent?.ToString() ?? string.Empty;
            }
            
            // update_fees
            // proxy
            // update_maxtasks
            // update_processor
            // update_admin
            // upgrade
            
            // bounce
            else if (inMsg.OpCode.Equals("0x0FFFFFFFF", StringComparison.InvariantCultureIgnoreCase)) 
            {
                op = "bounce";
            }
            
            // other
            else if (string.IsNullOrWhiteSpace(inMsg.OpCode)) 
            {
                op = "";
            }
            // 
            else
            {
                op = inMsg.OpCode.ToLowerInvariant();
            }
        }
        catch (Exception e)
        {
            comment = "Error occured during transaction processing";
        }
        
        return new MarketingTransactionMessageResponse
        {
            Value = inMsg.Value.ToDecimal(),
            Op =  op,
            Comment = comment,
            ProfileAddr = profileAddr,
            FromAddr = inMsg.Source.ToString(),
            ToAddr = inMsg.Destination.ToString(),
            QueryId = queryId,
            Key = key,
            M = m,
            ParentAddr =  parentAddr,
            Pos = pos,
            PosSet = posSet,
        };
    }
    
    private static MarketingTransactionMessageResponse CreateFromOutMsg(RawMessage outMsg)
    {
        var op = string.Empty;
        var comment = string.Empty;
        var profileAddr = string.Empty;
        
        ulong queryId = 0;
        ulong key = 0;
        ulong m = 0;
        
        var parentAddr = string.Empty;
        ulong pos = 0;
        var posSet = false;
        
        try
        {
             // bonus
            if (outMsg.OpCode.Equals("0x39cb9dfb", StringComparison.InvariantCultureIgnoreCase)) // should be 
            {
                // bonus#39cb9dfb  query_id:uint64  comment:^Cell = ProfileInternalMsg;
                op = "bonus";
                profileAddr = outMsg.Destination.ToString();
                
                var slice = outMsg.MsgData.Body.Parse();
                var opCode = slice.LoadUInt(32);
                queryId = (ulong)slice.LoadUInt(64);
                
                var msgSlice = slice.LoadRef().Parse();
                comment = msgSlice.LoadString();
            }
            
            // excesses
            else if (outMsg.OpCode.Equals("0x7D7AEC1D", StringComparison.InvariantCultureIgnoreCase)) 
            {
                // excesses#7d7aec1d query_id:uint64 = MultiExcessesMsg;
                
                op = "excesses";
                
                var slice = outMsg.MsgData.Body.Parse();
                var opCode = slice.LoadUInt(32);
                queryId = (ulong)slice.LoadUInt(64);
            }
            
            // refund
            else if (outMsg.OpCode.Equals("0x0C135F40C", StringComparison.InvariantCultureIgnoreCase)) 
            {
                // refund#c135f40c  query_id:uint64 = MultiRefundMsg;
                
                op = "refund";
                var slice = outMsg.MsgData.Body.Parse();
                var opCode = slice.LoadUInt(32);
                queryId = (ulong)slice.LoadUInt(64);
            }
            
            // add_child
            else if (outMsg.OpCode.Equals("0x0CEA08332", StringComparison.InvariantCultureIgnoreCase))  // should be 0xcea08332
            {
                // add_child#cea08332 query_id:uint64  profiles:^PlaceProfiles = PlaceInternalMsg;
                
                op = "add_child";
                var slice = outMsg.MsgData.Body.Parse();
                var opCode = slice.LoadUInt(32);
                queryId = (ulong)slice.LoadUInt(64);
            }
            
            // other
            else if (string.IsNullOrWhiteSpace(outMsg.OpCode)) 
            {
                op = "";
            }
          
            else
            {
                op = outMsg.OpCode.ToLowerInvariant();
            }
        }
        catch (Exception e)
        {
            comment = "Error occured during transaction processing";
        }
        
        return new MarketingTransactionMessageResponse
        {
            FromAddr = outMsg.Source.ToString(),
            ToAddr =  outMsg.Destination.ToString(),
            Value = outMsg.Value.ToDecimal() * -1m,
            Op =  op,
            Comment = comment,
            ProfileAddr = profileAddr,
            
            QueryId = queryId,
            Key = key,
            M = m,
            
            Pos =  pos,
            PosSet = posSet,
        };
    }
    
    
    public static MarketingTransactionMessageResponse[] Create(TransactionsInformationResult transaction)
    {
        List<MarketingTransactionMessageResponse> messages = [];

        if (transaction.InMsg.Source is not null) // skip external messages
        {
            messages.Add(CreateFromInMsg(transaction.InMsg));
        }

        foreach (var outMsg in transaction.OutMsgs)
        {
            messages.Add(CreateFromOutMsg(outMsg));
        }

        return messages.ToArray();
    }
}
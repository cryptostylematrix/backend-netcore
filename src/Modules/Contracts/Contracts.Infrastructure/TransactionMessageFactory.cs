namespace Contracts.Infrastructure;


internal static class TransactionMessageFactory
{
    
    private static TransactionMessageResponse CreateFromInMsg(RawMessage inMsg)
    {
        string op;
        var comment = string.Empty;
        var profileAddr =  string.Empty;
       
        // bonus
        if (string.IsNullOrWhiteSpace(inMsg.OpCode) && inMsg.Message.StartsWith("Crypto Style", StringComparison.InvariantCulture))
        {
            op = "bonus";
            comment =  inMsg.Message;
            profileAddr = inMsg.Source.ToString();
        }
        
        // excesses
        else if (inMsg.OpCode.Equals("0x0d53276db", StringComparison.InvariantCultureIgnoreCase)) // should be 0xd53276db
        {
            op = "excesses";
            profileAddr = inMsg.Source.ToString();
        }
        
        // ownership_assigned
        else if (inMsg.OpCode.Equals("0x5138D91", StringComparison.InvariantCultureIgnoreCase)) // should be 0x05138d91
        {
            op = "ownership_assigned";
            profileAddr = inMsg.Source.ToString();
        }
        
        // bounce
        else if (inMsg.OpCode.Equals("0x0FFFFFFFF", StringComparison.InvariantCultureIgnoreCase)) 
        {
            op = "bounce";
        }
        
        // refund
        else if (inMsg.OpCode.Equals("0x0C135F40C", StringComparison.InvariantCultureIgnoreCase)) 
        {
            op = "refund";
        }
        
        // ?
        else if (string.IsNullOrWhiteSpace(inMsg.OpCode)) 
        {
            op = "";
        }
        // 
        else
        {
            op = inMsg.OpCode.ToLowerInvariant();
        }
        
        return new TransactionMessageResponse
        {
            Addr = inMsg.Source.ToString(),
            Value = inMsg.Value.ToDecimal(),
            Op =  op,
            Comment = comment,
            ProfileAddr = profileAddr
        };
    }
    
    private static TransactionMessageResponse CreateFromOutMsg(RawMessage outMsg)
    {
        string op;
        var comment = string.Empty;
        var profileAddr = string.Empty;
        
        // buy_place
        if (outMsg.OpCode.Equals("0x179b74a8", StringComparison.InvariantCultureIgnoreCase))
        {
            op = "buy_place";
            
            // buy_place#179b74a8  query_id:uint64  m:Matrix  profile:Address  pos:(Maybe ^PlacePos) = MultiInternalMsg;
            
            var slice = outMsg.MsgData.Body.Parse();
            var opCode = slice.LoadUInt(32);
            var queryId = slice.LoadUInt(64);
            var m = slice.LoadUInt(3);
            var profile = slice.LoadAddress();
            
            profileAddr = profile?.ToString() ?? string.Empty;
            comment = $"m:{m}";
        }
        
        // lock_pos
        else if (outMsg.OpCode.Equals("0x6d31ad42", StringComparison.InvariantCultureIgnoreCase))
        {
            op = "lock_pos";
            
            // lock_pos#6d31ad42  query_id:uint64  m:Matrix  profile:Address  pos:^PlacePos = MultiInternalMsg;
            
            var slice = outMsg.MsgData.Body.Parse();
            var opCode = slice.LoadUInt(32);
            var queryId = slice.LoadUInt(64);
            var m = slice.LoadUInt(3);
            var profile = slice.LoadAddress();
            
            profileAddr = profile?.ToString() ?? string.Empty;
            comment = $"m:{m}";
        }
        
        // unlock_pos
        else if  (outMsg.OpCode.Equals("0x77d27591", StringComparison.InvariantCultureIgnoreCase))
        {
            op = "unlock_pos";
            
            // unlock_pos#77d27591  query_id:uint64  m:Matrix  profile:Address  pos:^PlacePos = MultiInternalMsg;
            
            var slice = outMsg.MsgData.Body.Parse();
            var opCode = slice.LoadUInt(32);
            var queryId = slice.LoadUInt(64);
            var m = slice.LoadUInt(3);
            var profile = slice.LoadAddress();
            
            profileAddr = profile?.ToString() ?? string.Empty;
            comment = $"m:{m}";
        }
        
        // deploy_nft_item
        else if (outMsg.OpCode.Equals("0x1", StringComparison.InvariantCultureIgnoreCase))
        {
            op = "deploy_nft_item";
            
            // deploy_nft_item#1  query_id:uint64  login:Cell = ProfileCollectionInternalMsg;
            
            var slice = outMsg.MsgData.Body.Parse();
            var opCode = slice.LoadUInt(32);
            var queryId = slice.LoadUInt(64);
            var login = slice.LoadString();
            
            comment = login;
        }
        
        // edit_content
        else if (outMsg.OpCode.Equals("0x1a0b9d51", StringComparison.InvariantCultureIgnoreCase))
        {
            op = "edit_content";
            profileAddr = outMsg.Destination.ToString();
        }
        
        // choose_inviter
        else if (outMsg.OpCode.Equals("0x0ef27e2d6", StringComparison.InvariantCultureIgnoreCase)) // should be 0xef27e2d6. Need to fix
        {
            op = "choose_inviter";
            
            profileAddr = outMsg.Destination.ToString();
        }
        
        // transfer
        else if (outMsg.OpCode.Equals("0x5fcc3d14", StringComparison.InvariantCultureIgnoreCase)) 
        {
            op = "transfer";
            
            profileAddr = outMsg.Destination.ToString();
        }
        
        else 
        {
            op = outMsg.OpCode.ToLowerInvariant();
        }
        
        return new TransactionMessageResponse
        {
            Addr = outMsg.Destination.ToString(),
            Value = outMsg.Value.ToDecimal() * -1m,
            Op =  op,
            Comment = comment,
            ProfileAddr = profileAddr
        };
    }
    
    
    public static TransactionMessageResponse[] Create(TransactionsInformationResult transaction)
    {
        List<TransactionMessageResponse> messages = [];

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
namespace UI.Dto;

public static class UiErrorCodes
{
    public const string WalletRequired = "err_wallet_not_connected";
    public const string InvalidWalletAddress = "err_invalid_wallet_address";
    public const string InvalidLogin = "err_invalid_login";
    public const string InvalidProfileMode = "err_invalid_profile_mode";
    public const string ProfileNotFound = "err_profile_not_found";
    public const string ContractRequestFailed = "err_contract_request_failed";
    public const string ProfileDoesNotBelongToWallet =
        "err_contract_doesnot_belong_to_the_wallet";
    public const string RelationshipNotFound = "err_profile_relationship_not_found";
}

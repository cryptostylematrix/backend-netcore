using System.Net.Sockets;
using System.Security.Authentication;
using Npgsql;

namespace LegacyPlacesXmindExporter;

public static class DatabaseError
{
    // Use only known error codes and fixed text, never driver messages or connection strings.
    public static string Describe(NpgsqlException exception)
    {
        if (exception is PostgresException postgres)
            return $"PostgreSQL rejected the request (SQLSTATE {postgres.SqlState}). Check database access, credentials, and SELECT permissions on public.places and public.partners.";

        for (Exception? cause = exception; cause is not null; cause = cause.InnerException)
        {
            if (cause is SocketException socket)
                return socket.SocketErrorCode switch
                {
                    SocketError.HostNotFound or SocketError.NoData or SocketError.TryAgain =>
                        "PostgreSQL hostname could not be resolved. Check Host, DNS, and VPN access.",
                    SocketError.ConnectionRefused =>
                        "PostgreSQL connection was refused. Check Host and Port, whether PostgreSQL is running and listening, and any required tunnel.",
                    SocketError.TimedOut =>
                        "PostgreSQL connection timed out. Check Host, Port, firewall rules, and VPN access.",
                    SocketError.NetworkUnreachable or SocketError.HostUnreachable =>
                        "PostgreSQL host is unreachable. Check network routing and VPN access.",
                    _ => $"PostgreSQL network error ({socket.SocketErrorCode}). Check network access and server availability."
                };
            if (cause is AuthenticationException)
                return "PostgreSQL TLS negotiation failed. Check SSL Mode, server certificate trust, and certificate hostname. This is a TLS error, not necessarily a database password error.";
            if (cause is TimeoutException)
                return "PostgreSQL operation timed out. Check server availability, network access, and the connection/query timeout settings.";
        }

        var types = new List<string>();
        for (Exception? cause = exception; cause is not null; cause = cause.InnerException)
            types.Add(cause.GetType().Name);
        return $"PostgreSQL operation failed ({string.Join(" -> ", types)}). Check server logs and connection settings. No output file has been created by the database operation.";
    }
}

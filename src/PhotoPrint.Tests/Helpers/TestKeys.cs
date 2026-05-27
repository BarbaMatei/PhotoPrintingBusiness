namespace PhotoPrint.Tests.Helpers;

/// <summary>
/// RSA key pair used across all auth tests. This is a test-only key — never use in production.
/// </summary>
public static class TestKeys
{
    /// <summary>2048-bit RSA private key in PKCS#1 PEM format (test only).</summary>
    public const string RsaPrivateKeyPem =
        "-----BEGIN RSA PRIVATE KEY-----\n" +
        "MIIEowIBAAKCAQEA3fuDdT0iuXiFZWHb32iCZ4oPMfsV3aWl0vA+MHOw1K2YD9gp\n" +
        "b3YNO9I6EhuB5HETfEPV/PJDcthULZ4xv8ryflS4YRbdlT7bUe3ogCh16ku5o3sS\n" +
        "rSx1MV05pLCTrZ9/xOMIhFjnd+CnVfYA+b21Ktaf+7Jq4Kts0IehEK1A5HlPWq3F\n" +
        "9nY7HDbwlZzuGpqT1YhIlYV7m7P0x+Uw8udF3O7F1CdqqjsudBopi1J42QEJUmZe\n" +
        "4H+5BGgz8wUgsomGegvlRer3gaIFbymryWky+ZikBq9Kp+dIfIQot2EprAoRCZ+X\n" +
        "t5z9BxiKhGfXrxIzQfwRfwjLyfo/1LRtaR/KXQIDAQABAoIBAH/8dczw1MaPAIp1\n" +
        "o6npvdYouZ4doHvM+BDox1J0Qa498ICiJzHGpNaq3SR3i6rEr3FkQ0q1+8GJjO9I\n" +
        "WHK2dp30BuKjplpYhWd1fin2KhZOVtg1K42BJoixsXyM+niOj8JbDBwSjlKFyrU7\n" +
        "Q1C62mu6lz8tnYWwEOsiZ/EPk2ymmfy2TE6l9/YxqDJbl+4M8gDS90yy6Bt8AGkR\n" +
        "I47axd352BYhHs9fKUMC4IcnmMUOropc+Yl17tMDuood7P8yPHjS1LDkZ/vRUb0m\n" +
        "96OdHOKGcEfvx458eHu7dBY71eNAQAxkOjkCaAqMuf6DOfiTWS6xCTWRvnT/WuFj\n" +
        "zM5rJFkCgYEA7AsHrBO7vYcI6WhhkEJhw3X01CwpiW9IMzVVqvSi09wMR6gIBEgf\n" +
        "4Ekr8LMFCwT+XyS4X1qvzsKSpJKQiBlEdySawyVpqFB30uzPUVyD0xtxAZ4y7EdR\n" +
        "HbmDUVX9NRDclNDKPonzv7vmgOB1LFjAHQLRQ6VzvzvcRYrX6b2iavsCgYEA8MAn\n" +
        "CTL/K9yTySv8md+dQnq5PqywC5qtLRyOiyfkBMkTA9RkTw9zsAnCXSUoPBvbLsx2\n" +
        "NqeYjEEjsT0/2hr2QWFX8nk5GHLVM4I56MAkXXHeLKf7DQxmemFO6uh4iX/qBkrF\n" +
        "jvak1By6MqXZpxjuJNAOSDPHLw/cC1RRPwYuIIcCgYEA0OTGCmyAVq+9nErrJO8K\n" +
        "dB/c5zSaIe3g+Ki3ww6zV0lDeNrlFVz7ENPQ1jioOuNVdsAZhxHLyvB5NLocvMWX\n" +
        "yNUVPaTLh9CG6pz2sKtuYpLDhMoLiP1odSTraTzvVFoyzGSmx4fwtntE+EMsj22P\n" +
        "v1zx86rl75S7ULQadtqDdacCgYBuhlilcYMSGN7EAWyjG7Svm3XF3zOm8CjGyBBs\n" +
        "tDCLPeB75prybM6Yp7JSXsec6ND0KCuxJbnz0cfYC51vvOcG1vCwQZTDs5xLXGLH\n" +
        "hsZrG+Z6q9emguXdEyVO1NDZlx7SKquN2Y+MTW/x5pAIlXpm7hlQbmzoHyjPDrOJ\n" +
        "8oVkqwKBgF3Cy32dOTpXBg4wBHN7ZIkWHrAUXNE/3Z7TobTkgRvz3Rt9gQF/1ZZa\n" +
        "69eiQVUXmFL8niRB9N0LT18vLH6ICgQSjo/ZkDf6lfDKwyLlxvUXJKPefeQ0HEAm\n" +
        "0eP6PfNve0ar/+QuuGP4ZbYEV5wwOolrAWk7aLm9naAz6iNy1kEG\n" +
        "-----END RSA PRIVATE KEY-----";
}

import com.google.gson.Gson;
import com.microsoft.aad.msal4j.*;

import java.io.FileNotFoundException;
import java.io.IOException;
import java.io.PrintWriter;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.Collections;
import java.util.Set;

public class TestConsoleApp {
    static String readFile(String path) throws IOException {
        return new String(Files.readAllBytes(Paths.get(path)));
    }

    static void writeFile(String path, String content) throws IOException {
        Files.write(Paths.get(path), content.getBytes());
    }

    static IAccount getAccount(IPublicClientApplication app, String upn) {
        Set<IAccount> accounts = app.getAccounts().join();

        for (IAccount account : accounts) {
            if (account.username().equals(upn)) {
                return account;
            }
        }
        return null;
    }

    public static void main(String args[]) throws Exception {
        String pathToInputConfiguration = args[1];

        System.out.println("pathToInputConfiguration - " + pathToInputConfiguration);

        TestInput testInput = new Gson().fromJson(readFile(pathToInputConfiguration), TestInput.class);

        System.out.println("CacheFilePath - " + testInput.cacheFilePath);
        System.out.println("ResultsFilePath - " + testInput.resultsFilePath);

        IPublicClientApplication app = PublicClientApplication.builder(testInput.clientId)
                .authority(testInput.authority)
                .setTokenCacheAccessAspect(new ITokenCacheAccessAspect() {
                    public void beforeCacheAccess(ITokenCacheAccessContext iTokenCacheAccessContext) {
                        try {
                            String data = new String(Files.readAllBytes(Paths.get(testInput.cacheFilePath)));
                            iTokenCacheAccessContext.tokenCache().deserialize(data);
                        } catch (IOException e) {
                            e.printStackTrace();
                        }
                    }

                    public void afterCacheAccess(ITokenCacheAccessContext iTokenCacheAccessContext) {
                        try (PrintWriter out = new PrintWriter(testInput.cacheFilePath)) {
                            out.println(iTokenCacheAccessContext.tokenCache().serialize());
                        } catch (FileNotFoundException e) {
                            e.printStackTrace();
                        }
                    }
                }).build();

        TestOutput testOutput = new TestOutput();

        for (TestInput.LabUserData user : testInput.users) {
            IAccount account = getAccount(app, user.upn);

            IAuthenticationResult result = null;
            Set<String> scopes = Collections.singleton(testInput.scope);

            if (account != null) {
                result = app.acquireTokenSilently(SilentParameters.builder(scopes, account)
                        .build())
                        .join();
                if (result != null) {
                    System.out.println("got token for (" + result.account().username() + ") from the cache");

                    testOutput.results.add
                            (new TestOutput.Result(user.upn, result.account().username(), true));
                }
            }

            if (result != null) {
                try {
                    result = app.acquireToken(UserNamePasswordParameters.builder
                            (scopes, user.upn, user.password.toCharArray()).build()).join();

                    testOutput.results.add
                            (new TestOutput.Result(user.upn, result.account().username(), false));

                    System.out.println("got token for (" + result.account().username() + ") by signing in with credentials");
                } catch (AuthenticationException ex) {
                    System.out.println("**TOKEN ACQUIRE FAILURE**");
                    System.out.println(ex.getMessage());
                    System.out.println(ex.getErrorCode());
                    testOutput.results.add
                            (new TestOutput.Result(user.upn, null, false));
                }
            }
        }
        writeFile(testInput.resultsFilePath, new Gson().toJson(testOutput));
    }
}

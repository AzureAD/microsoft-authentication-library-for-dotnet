// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

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
        return new String(Files.readAllBytes(Paths.get(path))); //CodeQL [SM00697] Non-production test app that reads from a known file - the file path is not an open input param. False positive.
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

        System.out.println("Java app, pathToInputConfiguration - " + pathToInputConfiguration);

        TestInput testInput = new Gson().fromJson(readFile(pathToInputConfiguration), TestInput.class);

        System.out.println("Java app, CacheFilePath - " + testInput.cacheFilePath);
        System.out.println("Java app, ResultsFilePath - " + testInput.resultsFilePath);


        TestOutput testOutput = new TestOutput();

        for (TestInput.LabUserData user : testInput.users) {
            System.out.println("Java app, user.clientId - " + user.clientId);        
            IPublicClientApplication app = PublicClientApplication.builder(user.clientId)
                    .authority(user.authority)
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

            System.out.println("Java app, process user - " + user.upn);

            IAccount account = getAccount(app, user.upn);

            IAuthenticationResult result = null;
            Set<String> scopes = Collections.singleton(testInput.scope);

            if (account != null) {
                System.out.println("Java app, found account for user - " + user.upn);
                System.out.println("Java app, account.username() - " + account.username());

                result = app.acquireTokenSilently(SilentParameters.builder(scopes, account)
                        .build())
                        .join();
                if (result != null) {
                    System.out.println("got token for (" + result.account().username() + ") from the cache");

                    testOutput.results.add
                            (new TestOutput.Result(user.upn, result.account().username(), true));
                }
            }

            if (result == null) {
                try {
                   System.out.println("Java app, acquire token interectively for user.upn - " + user.upn);

                    result = app.acquireToken(UserNamePasswordParameters.builder
                            (scopes, user.upn, user.password.toCharArray()).build()).join();

                    testOutput.results.add
                            (new TestOutput.Result(user.upn, result.account().username(), false));

                    System.out.println("got token for (" + result.account().username() + ") by signing in with credentials");
                } catch (MsalException ex) {
                    System.out.println("**TOKEN ACQUIRE FAILURE**");
                    System.out.println(ex.getMessage());
                    System.out.println(ex.errorCode());
                    testOutput.results.add
                            (new TestOutput.Result(user.upn, null, false));
                }
            }
        }
       System.out.println("Java app, write output to - " + testInput.resultsFilePath);
       System.out.println("Java app, outputy - " + new Gson().toJson(testOutput));

        writeFile(testInput.resultsFilePath, new Gson().toJson(testOutput));
    }
}

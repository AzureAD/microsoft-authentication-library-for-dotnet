/*
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License.
 */


/* To run this locally, start with `node index.js --inputPath c:\\temp\\input.json` where input.json contains

{
    "Scope": "https://graph.microsoft.com/user.read",
    "CacheFilePath": "C:\\Users\\bogavril\\AppData\\Local\\Temp\\adalcachecompattestdata\\msalCacheV3.binx",
    "LabUserDatas": [
        {
            "Upn": "liu.kang@bogavrilltd.onmicrosoft.com",
            "Password": "****",
            "ClientId": "1d18b3b0-251b-4714-a02a-9956cec86c2d",
            "TenantId": "49f548d0-12b7-4169-a390-bb5304d24462",
            "Authority": "https://login.microsoftonline.com/49f548d0-12b7-4169-a390-bb5304d24462/"
        }
    ],
    "ResultsFilePath": "C:\\Users\\bogavril\\AppData\\Local\\Temp\\adalcachecompattestdata\\msal_python_results.json",
    "StorageType": 4
}

Example output (should go in ResultsFilePath):

{
    "ErrorMessage": "", 
    "IsError": false, 
    "Results": [
        {
            "AuthResultUpn": "liu.kang@bogavrilLTD.onmicrosoft.com", 
            "IsAuthResultFromCache": false, 
            "LabUserUpn": "liu.kang@bogavrilltd.onmicrosoft.com"
        }
    ]
}

*/


var msal = require("@azure/msal-node");
const { Console } = require("console");
const { promises: fs } = require("fs");

async function readTestInputAsync() {

    if (process.argv.length < 4) {
        console.error("No arguments given. Expecting an input file path");
        throw "No arguments given. Expecting an input file path.";
    }
    let inputFile = process.argv[3];

    console.log("Reading input file: " + inputFile);

    let rawInputData = await fs.readFile(inputFile);
    let testInput = JSON.parse(rawInputData);
    console.log("CacheFilePath: " + testInput.CacheFilePath);

    return testInput;
}

async function createPCAAsync(testInput, userIndex) {
    const cachePath = testInput.CacheFilePath;


    const beforeCacheAccess = async (cacheContext) => {
        const fileExists = async path => !!(await fs.stat(path).catch(e => false));

        let cacheFileExists = await fileExists(cachePath);
        if (cacheFileExists) {
            cacheContext.tokenCache.deserialize(await fs.readFile(cachePath, "utf-8"));
        }
    };

    const afterCacheAccess = async (cacheContext) => {
        if (cacheContext.cacheHasChanged) {
            await fs.writeFile(cachePath, cacheContext.tokenCache.serialize());
        }
    };

    const cachePlugin = {
        beforeCacheAccess,
        afterCacheAccess
    };

    const msalConfig = {
        auth: {
            clientId: testInput.LabUserDatas[userIndex].ClientId,
            authority: testInput.LabUserDatas[userIndex].Authority,
        },
        cache: {
            cachePlugin
        }
    };

    const pca = new msal.PublicClientApplication(msalConfig);
    return pca;
}

async function tokenCallsAsync(testInput) {

    const results = [];
    for (i = 0; i < testInput.LabUserDatas.length; i++)
    {
        const pca = await createPCAAsync(testInput, i);    

        try {
            const msalTokenCache = pca.getTokenCache();
            accounts = await msalTokenCache.getAllAccounts();
            let account = null;
            if (accounts.length > 0) {
                account = accounts[0];
            }

            const silentRequest = {
                account: account,
                scopes: testInput.Scope,
            };

            const silentResult = await pca.acquireTokenSilent(silentRequest);
            console.log("Got a token from the cache!");
            results.push({ 
                "AuthResultUpn": silentRequest.account.username, 
                "IsAuthResultFromCache": true, 
                "LabUserUpn":  testInput.LabUserDatas[i].Upn});

        } catch (err) {

            const usernamePasswordRequest = {
                scopes: [testInput.Scope],
                username: testInput.LabUserDatas[i].Upn,
                password: testInput.LabUserDatas[i].Password,
            };

            const ropcResult = await pca.acquireTokenByUsernamePassword(usernamePasswordRequest);
            console.log("Got a token using ROPC!");
            results.push({ 
                "AuthResultUpn": ropcResult.account.username, 
                "IsAuthResultFromCache": false, 
                "LabUserUpn":  testInput.LabUserDatas[i].Upn});
        }
    }

    return results;
}

async function saveSuccessResultAsync(testInput, results) {
    const fullResult = {
        "ErrorMessage": "", 
        "IsError": false, 
        "Results": results
    };

    console.log("Saving the results to " + testInput.ResultsFilePath);
    await fs.writeFile(testInput.ResultsFilePath, JSON.stringify(fullResult));
}

async function saveFailureResultAsync(testInput, error) {
    const fullResult = {
        "ErrorMessage": error.message, 
        "IsError": true, 
        "Results": null
    };

    console.log("Saving the failure to " + testInput.ResultsFilePath);
    await fs.writeFile(testInput.ResultsFilePath,  JSON.stringify(fullResult));

}

(async () => {
    console.log("Starting...");

    const testInput = await readTestInputAsync();

    try {
        const results = await tokenCallsAsync(testInput);
        await saveSuccessResultAsync(testInput, results);

    } catch (err) {
        await saveFailureResultAsync(testInput, err);
        
    }
})();





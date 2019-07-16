#-----------------------------------------------------------------------------------------
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License. See LICENSE in the project root for license information.
#-----------------------------------------------------------------------------------------

import sys
import os
import atexit
import json
import msal

class TestInputData:
    def __init__(self, labUserDatas, resultsFilePath, storageType):
        self.labUserDatas = labUserDatas
        self.resultsFilePath = resultsFilePath
        self.storageType = storageType

class CacheExecutorAccountResult:
    def __init__(self, labUserUpn, authResultUpn, isAuthResultFromCache):
        self.LabUserUpn = labUserUpn
        self.AuthResultUpn = authResultUpn
        self.IsAuthResultFromCache = isAuthResultFromCache

    def toJSON(self):
        return json.dumps(self, default=lambda o: o.__dict__, sort_keys=True, indent=4)

class ExecutionContext:
    def __init__(self, isError, errorMessage, results):
        self.IsError = isError
        self.ErrorMessage = errorMessage
        self.Results = results

    def toJSON(self):
        return json.dumps(self, default=lambda o: o.__dict__, sort_keys=True, indent=4)

cmdlineargs = {}
cmdlineargs["inputFilePath"] = sys.argv[2]

print(os.path.dirname(os.path.realpath(__file__)))
print(sys.argv[2])
with open(sys.argv[2], 'r') as fp:
    testInput = json.load(fp)

the_scopes = [ testInput['Scope'] ]

cache = msal.SerializableTokenCache()

cacheFilePath = testInput['CacheFilePath']
print('CacheFilePath: ' + cacheFilePath)

resultsFilePath = testInput['ResultsFilePath']
print('ResultsFilePath: ' + resultsFilePath)

atexit.register(lambda:
    open(cacheFilePath, 'w').write(cache.serialize())
)

if os.path.exists(cacheFilePath):
    cache.deserialize(open(cacheFilePath, 'r').read())

app = msal.PublicClientApplication(testInput['ClientId'], authority=testInput['Authority'], token_cache=cache)

results = []

for labUserData in testInput['LabUserDatas']:
    upn = labUserData['Upn']
    print('Handling labUserData.Upn = ' + upn)
    accounts = app.get_accounts(username=upn)

    result = None

    if accounts:
        result = app.acquire_token_silent(the_scopes, account=accounts[0])

    if result:
        print("got token for '" + upn + "' from the cache")
        results.append(CacheExecutorAccountResult(upn, accounts[0]["username"] if accounts else "n/a", True))
    else:
        result = app.acquire_token_by_username_password(upn, labUserData['Password'], scopes=the_scopes)
        if result:
            print("got token for '" + upn + "' by signing in with credentials")
            print(result)
            results.append(CacheExecutorAccountResult(upn, result.get("id_token_claims", {}).get("preferred_username"), False))
        else:
            print("** ACQUIRE TOKEN FAILURE **")
            print(result.get("error"))
            print(result.get("error_description"))
            print(result.get("correlation_id")) 
            results.append(CacheExecutorAccountResult(upn, '', False))

executionContext = ExecutionContext(False, '', results)
json = executionContext.toJSON()

with open(resultsFilePath, 'w') as outfile:
    outfile.write(json)

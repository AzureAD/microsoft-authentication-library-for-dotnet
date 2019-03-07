# ------------------------------------------------------------------------------
# 
# Copyright (c) Microsoft Corporation.
# All rights reserved.
# 
# This code is licensed under the MIT License.
# 
# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files(the "Software"), to deal
# in the Software without restriction, including without limitation the rights
# to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
# copies of the Software, and to permit persons to whom the Software is
# furnished to do so, subject to the following conditions :
# 
# The above copyright notice and this permission notice shall be included in
# all copies or substantial portions of the Software.
# 
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
# THE SOFTWARE.
# 
# ------------------------------------------------------------------------------

import sys
import os
import atexit
import msal

config = {}

config["client_id"] = sys.argv[1]
config["authority"] = sys.argv[2]
config["scope"] = sys.argv[3]
config["username"] = sys.argv[4]
config["password"] = sys.argv[5]
config["cache_path"] = sys.argv[6]

cache = msal.SerializableTokenCache()

if os.path.exists(config["cache_path"]):
    cache.deserialize(open(config["cache_path"], "r").read())

atexit.register(lambda:
    open(config["cache_path"], "w").write(cache.serialize())
)

the_scopes = [ config["scope"] ]

# Create a preferably long-lived app instance which maintains a token cache.
app = msal.PublicClientApplication(config["client_id"], authority=config["authority"], token_cache=cache)

# The pattern to acquire a token looks like this.
result = None

# Firstly, check the cache to see if this end user has signed in before
accounts = app.get_accounts(username=config["username"])
if accounts:
    result = app.acquire_token_silent(the_scopes, account=accounts[0])

if result:
    print("**TOKEN RECEIVED FROM CACHE**")
else:
    result = app.acquire_token_by_username_password(config["username"], config["password"], scopes=the_scopes)
    if result:
        print("**TOKEN NOT RECEIVED FROM CACHE**")
    else:
        print("**TOKEN ACQUIRE FAILURE**")
        print(result.get("error"))
        print(result.get("error_description"))
        print(result.get("correlation_id"))  # You may need this when reporting a bug

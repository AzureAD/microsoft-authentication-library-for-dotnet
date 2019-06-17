// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

import com.google.gson.annotations.SerializedName;

import java.util.ArrayList;
import java.util.List;

public class TestOutput {

    @SerializedName("ErrorMessage")
    String errorMessage;

    @SerializedName("IsError")
    String isError;

    @SerializedName("Results")
    List<Result> results = new ArrayList<>();

    static class Result{
        Result(String labUserUpn, String authResultUpn, boolean isAuthResultFromCache){
            this.authResultUpn = authResultUpn;
            this.isAuthResultFromCache = isAuthResultFromCache;
            this.labUserUpn = labUserUpn;
        }

        @SerializedName("AuthResultUpn")
        String authResultUpn;

        @SerializedName("IsAuthResultFromCache")
        boolean isAuthResultFromCache;

        @SerializedName("LabUserUpn")
        String labUserUpn;
    }
}

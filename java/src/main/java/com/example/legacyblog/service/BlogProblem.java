package com.example.legacyblog.service;

public class BlogProblem extends RuntimeException {
    private final int status;
    public BlogProblem(int status, String message) { super(message); this.status = status; }
    public int getStatus() { return status; }
}

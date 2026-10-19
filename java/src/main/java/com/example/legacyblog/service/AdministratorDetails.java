package com.example.legacyblog.service;

import com.example.legacyblog.model.Administrator;
import org.springframework.security.core.userdetails.*;
import org.springframework.stereotype.Service;

@Service
public class AdministratorDetails implements UserDetailsService {
    private final BlogService blogs;
    public AdministratorDetails(BlogService blogs) { this.blogs = blogs; }
    @Override
    public UserDetails loadUserByUsername(String username) {
        Administrator value = blogs.administrator(username);
        if (value == null) { throw new UsernameNotFoundException("Invalid credentials."); }
        return User.withUsername(value.getUsername()).password(value.getPasswordHash()).roles("ADMIN").build();
    }
}

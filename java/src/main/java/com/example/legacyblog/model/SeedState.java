package com.example.legacyblog.model;

import javax.persistence.*;

@Entity
@Table(name = "seed_state")
public class SeedState {
    @Id @Column(length = 32)
    private String id;
    public SeedState() { }
    public SeedState(String id) { this.id = id; }
}

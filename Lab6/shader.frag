#version 330

uniform float time;
uniform vec3 WatcherPos, TransparencyPos;

const vec3 Ka = vec3(0.1, 0.1, 0.2);
const vec3 Kd = vec3(1.0, 1.0, 0.4);
const vec3 Ks = vec3(0.2, 0.2, 1.0);
const vec3 Ia = vec3(1.0, 1.0, 1.0);
const vec3 Il1 = vec3(0.3, 0.3, 0.2);
const vec3 Il2 = vec3(0.2, 0.3, 0.3);
const float P = 7;
const float K1 = 0.4;
const float K2 = 0.1;

varying vec3 position;
varying vec3 normal;
varying vec3 color;

const float Pi = 3.1415926;

vec3 bound(vec3 v) {
    return min(max(v, 0.0), 1.0);
}

void main() {
    vec3 lightPos1 = vec3(1.5 * cos(time + Pi), 1.5 * sin(time + Pi), 1);
    vec3 i = Ia * Ka;
    float d = length(lightPos1 - position);
    vec3 L = normalize(lightPos1 - position);
    vec3 R = normalize(reflect(-L, normal));
    vec3 S = normalize(WatcherPos - position);
    float cosRS = dot(R, S);
    float LN = dot(L, normal);
    if (LN > 0) {
        i += Il1 * Kd * LN / (d * K1 + K2);
    }
    if (cosRS > 0) {
        i += Il1 * Ks * pow(cosRS, P) / (d * K1 + K2);
    }
    
    vec3 lightPos2 = vec3(1.5 * cos(time), 1.5 * sin(time), -1);
    d = length(lightPos2 - position);
    L = normalize(lightPos2 - position);
    R = normalize(reflect(-L, normal));
    S = normalize(WatcherPos - position);
    cosRS = dot(R, S);
    LN = dot(L, normal);
    if (LN > 0) {
        i += Il2 * Kd * LN / (d * K1 + K2);
    }
    if (cosRS > 0) {
        i += Il2 * Ks * pow(cosRS, P) / (d * K1 + K2);
    }
    
    float transparencyDist = length(TransparencyPos - position);
    float alpha = transparencyDist * transparencyDist / 1.5;
    gl_FragColor = vec4(bound(color * i), alpha);
}
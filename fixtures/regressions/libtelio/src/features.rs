use std::sync::Arc;
use std::{collections::HashSet, fmt, net::IpAddr, str::FromStr};

use num_enum::{IntoPrimitive, TryFromPrimitive};
use serde::{de::IntoDeserializer, Deserialize, Deserializer, Serialize};
use smart_default::SmartDefault;
use strum_macros::EnumCount;

use parking_lot::Mutex;

pub type EndpointProviders = HashSet<EndpointProvider>;

#[derive(Clone, Debug, PartialEq, Eq, Serialize, Deserialize, SmartDefault)]
#[serde(default)]
pub struct Features {
    #[serde(deserialize_with = "FeatureWireguard::default_on_null")]
    pub wireguard: FeatureWireguard,
    #[default(Some(Default::default()))]
    pub nurse: Option<FeatureNurse>,
    pub lana: Option<FeatureLana>,
    pub paths: Option<FeaturePaths>,
    pub direct: Option<FeatureDirect>,
    pub is_test_env: Option<bool>,
    #[serde(alias = "hide_ips")]
    #[default(true)]
    pub hide_user_data: bool,
    #[default(true)]
    pub hide_thread_id: bool,
    pub derp: Option<FeatureDerp>,
    pub validate_keys: FeatureValidateKeys,
    pub ipv6: bool,
    pub nicknames: bool,
    pub firewall: FeatureFirewall,
    pub flush_events_on_stop_timeout_seconds: Option<u64>,
    pub post_quantum_vpn: FeaturePostQuantumVPN,
    pub link_detection: Option<FeatureLinkDetection>,
    pub dns: FeatureDns,
    pub multicast: bool,
    pub batching: Option<FeatureBatching>,
}

impl Features {
    pub fn serialize(&self) -> Result<String, serde_json::Error> {
        serde_json::to_string(self)
    }
}

#[derive(Clone, Copy, Debug, PartialEq, Eq, Serialize, Deserialize, SmartDefault)]
#[serde(default)]
pub struct FeatureBatching {
    #[default(0)]
    pub direct_connection_threshold: u32,
    #[default(10)]
    pub trigger_effective_duration: u32,
    #[default(60)]
    pub trigger_cooldown_duration: u32,
}

#[derive(Clone, Debug, Default, PartialEq, Eq, Serialize, Deserialize)]
pub struct FeatureWireguard {
    #[serde(default)]
    pub persistent_keepalive: FeaturePersistentKeepalive,
    #[serde(default)]
    pub polling: FeaturePolling,
    #[serde(default)]
    pub enable_dynamic_wg_nt_control: bool,
    #[serde(default)]
    pub skt_buffer_size: Option<u32>,
}

impl FeatureWireguard {
    fn default_on_null<'de, D>(deserializer: D) -> Result<FeatureWireguard, D::Error>
    where
        D: Deserializer<'de>,
    {
        let opt = Option::deserialize(deserializer)?;
        Ok(opt.unwrap_or_default())
    }
}

#[derive(Clone, Debug, PartialEq, Eq, Serialize, Deserialize, SmartDefault)]
#[serde(default)]
pub struct FeaturePersistentKeepalive {
    #[default(Some(25))]
    pub vpn: Option<u32>,
    #[default(5)]
    pub direct: u32,
    #[default(Some(25))]
    pub proxying: Option<u32>,
    #[default(Some(25))]
    pub stun: Option<u32>,
}

#[derive(Clone, Debug, PartialEq, Eq, Serialize, Deserialize, SmartDefault)]
#[serde(default)]
pub struct FeaturePolling {
    #[default(1000)]
    pub wireguard_polling_period: u32,
    #[default(50)]
    pub wireguard_polling_period_after_state_change: u32,
}

#[derive(Clone, Debug, PartialEq, Eq, Serialize, Deserialize, SmartDefault)]
#[serde(default)]
pub struct FeatureNurse {
    #[default(60 * 60)]
    pub heartbeat_interval: u64,
    #[default(5 * 60)]
    pub initial_heartbeat_interval: u64,
    #[default(Some(Default::default()))]
    pub qos: Option<FeatureQoS>,
    pub enable_nat_type_collection: bool,
    #[default(true)]
    pub enable_relay_conn_data: bool,
    #[default(true)]
    pub enable_nat_traversal_conn_data: bool,
    #[default(60 * 60 * 24)]
    pub state_duration_cap: u64,
}

#[derive(Clone, Debug, PartialEq, Eq, Serialize, Deserialize, SmartDefault)]
#[serde(default)]
pub struct FeatureQoS {
    #[default(5 * 60)]
    pub rtt_interval: u64,
    #[default(3)]
    pub rtt_tries: u32,
    #[default(vec![RttType::Ping])]
    pub rtt_types: Vec<RttType>,
    #[default(5)]
    pub buckets: u32,
}

#[derive(Eq, PartialEq, Debug, Clone, Serialize, Deserialize)]
#[repr(u32)]
pub enum RttType {
    Ping,
}

#[derive(Clone, Default, PartialEq, Eq, Serialize, Deserialize)]
pub struct FeatureLana {
    pub event_path: String,
    pub prod: bool,
}

impl fmt::Debug for FeatureLana {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        f.debug_struct("FeatureLana")
            .field("prod", &self.prod)
            .finish()
    }
}

#[derive(Clone, Debug, Default, PartialEq, Eq, Serialize, Deserialize)]
pub struct FeaturePaths {
    pub priority: Vec<PathType>,
    pub force: Option<PathType>,
}

impl FeaturePaths {
    pub fn paths(&self) -> Vec<PathType> {
        if let Some(path) = self.force {
            vec![path]
        } else {
            [PathType::Relay]
                .iter()
                .chain(self.priority.iter())
                .fold(Vec::new(), |mut v, p| {
                    if !v.contains(p) {
                        v.push(*p);
                    }
                    v
                })
        }
    }
}

#[derive(Clone, Copy, Debug, Default, EnumCount, PartialEq, Eq, Hash, Serialize, Deserialize)]
#[serde(rename_all = "kebab-case")]
pub enum PathType {
    #[default]
    Relay,
    Direct,
}

#[derive(Clone, Debug, PartialEq, Eq, Serialize, Deserialize, SmartDefault)]
#[serde(default)]
pub struct FeatureDirect {
    #[serde(deserialize_with = "deserialize_providers")]
    pub providers: Option<EndpointProviders>,
    #[default = 25]
    pub endpoint_interval_secs: u64,
    #[default(Some(Default::default()))]
    pub skip_unresponsive_peers: Option<FeatureSkipUnresponsivePeers>,
    #[default(Some(Default::default()))]
    pub endpoint_providers_optimization: Option<FeatureEndpointProvidersOptimization>,
    #[default(Some(Default::default()))]
    pub upnp_features: Option<FeatureUpnp>,
}

fn deserialize_providers<'de, D>(de: D) -> Result<Option<EndpointProviders>, D::Error>
where
    D: Deserializer<'de>,
{
    let eps: Vec<&str> = match Deserialize::deserialize(de) {
        Ok(vec) => vec,
        Err(_) => return Ok(None),
    };

    let eps: HashSet<_> = eps
        .into_iter()
        .filter_map(|provider| {
            EndpointProvider::deserialize(<&str as IntoDeserializer>::into_deserializer(provider))
                .map_err(|e| {
                    println!("Failed to parse EndpointProvider: {}", e);
                })
                .ok()
        })
        .collect();

    Ok(Some(eps))
}

#[derive(
    Clone,
    Copy,
    Debug,
    EnumCount,
    IntoPrimitive,
    TryFromPrimitive,
    PartialEq,
    Eq,
    Hash,
    Serialize,
    Deserialize,
)]
#[repr(u32)]
#[serde(rename_all = "kebab-case")]
pub enum EndpointProvider {
    Local = 1,
    Stun = 2,
    Upnp = 3,
}

#[derive(Clone, Debug, PartialEq, Eq, Serialize, Deserialize, SmartDefault)]
#[serde(default)]
pub struct FeatureSkipUnresponsivePeers {
    #[default = 180]
    pub no_rx_threshold_secs: u64,
}

#[derive(Clone, Debug, PartialEq, Eq, Serialize, Deserialize, SmartDefault)]
#[serde(default)]
pub struct FeatureEndpointProvidersOptimization {
    #[default = true]
    pub optimize_direct_upgrade_stun: bool,
    #[default = true]
    pub optimize_direct_upgrade_upnp: bool,
}

/// Configure derp behaviour
#[derive(Clone, Debug, Default, PartialEq, Eq, Serialize, Deserialize)]
pub struct FeatureDerp {
    pub tcp_keepalive: Option<u32>,
    pub derp_keepalive: Option<u32>,
    #[serde(default)]
    pub poll_keepalive: Option<bool>,
    pub enable_polling: Option<bool>,
    #[serde(default)]
    pub use_built_in_root_certificates: bool,
}

/// Whether to validate keys
#[derive(Copy, Clone, Debug, PartialEq, Eq, Serialize, Deserialize)]
#[serde(transparent)]
pub struct FeatureValidateKeys(pub bool);

impl Default for FeatureValidateKeys {
    fn default() -> Self {
        Self(true)
    }
}

#[derive(Clone, Copy, Debug, PartialEq, Eq, Serialize, Deserialize)]
pub enum IpProtocol {
    /// UDP protocol
    UDP,
    /// TCP protocol
    TCP,
}

#[derive(Clone, Copy, Debug, PartialEq, Eq, Serialize, Deserialize)]
pub struct FirewallBlacklistTuple {
    pub protocol: IpProtocol,
    pub ip: IpAddr,
    pub port: u16,
}

#[derive(Default, Copy, Clone, Debug, PartialEq, Eq, Serialize, Deserialize)]
#[serde(transparent)]
pub struct Ipv4Net(ipnet::Ipv4Net);

impl From<Ipv4Net> for ipnet::Ipv4Net {
    fn from(value: Ipv4Net) -> Self {
        value.0
    }
}

impl From<ipnet::Ipv4Net> for Ipv4Net {
    fn from(value: ipnet::Ipv4Net) -> Self {
        Self(value)
    }
}

impl FromStr for Ipv4Net {
    type Err = ipnet::AddrParseError;
    fn from_str(s: &str) -> Result<Self, Self::Err> {
        s.parse().map(Self)
    }
}

impl fmt::Display for Ipv4Net {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        self.0.fmt(f)
    }
}

#[derive(Default, Clone, Debug, PartialEq, Eq, Serialize, Deserialize)]
pub struct FeatureFirewall {
    #[serde(default)]
    pub neptun_reset_conns: bool,
    #[serde(default)]
    pub boringtun_reset_conns: bool,
    pub exclude_private_ip_range: Option<Ipv4Net>,
    #[serde(default)]
    pub outgoing_blacklist: Vec<FirewallBlacklistTuple>,
}

#[derive(Clone, Copy, Debug, PartialEq, Eq, Serialize, Deserialize, SmartDefault)]
#[serde(default)]
pub struct FeaturePostQuantumVPN {
    #[default = 8]
    pub handshake_retry_interval_s: u32,
    #[default = 90]
    pub rekey_interval_s: u32,
    #[default = 1]
    pub version: u32,
}

#[derive(Copy, Clone, Debug, PartialEq, Eq, Serialize, Deserialize, SmartDefault)]
#[serde(default)]
pub struct FeatureLinkDetection {
    #[default = 15]
    pub rtt_seconds: u64,
    #[default = 0]
    pub no_of_pings: u32,
    #[default = false]
    pub use_for_downgrade: bool,
}

#[derive(Default, Clone, Debug, PartialEq, Eq, Serialize, Deserialize)]
pub struct FeatureDns {
    #[serde(default)]
    pub ttl_value: TtlValue,
    #[serde(default)]
    pub exit_dns: Option<FeatureExitDns>,
}

#[derive(Copy, Clone, Debug, PartialEq, Eq, Serialize, Deserialize)]
#[serde(transparent)]
pub struct TtlValue(pub u32);

impl Default for TtlValue {
    fn default() -> Self {
        Self(60)
    }
}

#[derive(Clone, Debug, Default, PartialEq, Eq, Serialize, Deserialize)]
pub struct FeatureExitDns {
    pub auto_switch_dns_ips: Option<bool>,
}

#[derive(Clone, Debug, PartialEq, Eq, Serialize, Deserialize, SmartDefault)]
#[serde(default)]
pub struct FeatureUpnp {
    #[default = 3600]
    pub lease_duration_s: u32,
}

pub struct FeaturesDefaultsBuilder {
    config: Mutex<Features>,
}

impl FeaturesDefaultsBuilder {
    pub fn new() -> Self {
        let config = Features {
            wireguard: default(),
            validate_keys: default(),
            firewall: default(),
            post_quantum_vpn: default(),
            dns: default(),
            nurse: None,
            lana: None,
            paths: None,
            direct: None,
            is_test_env: None,
            hide_user_data: true,
            hide_thread_id: true,
            derp: None,
            link_detection: None,
            flush_events_on_stop_timeout_seconds: None,
            multicast: false,
            ipv6: false,
            nicknames: false,
            batching: None,
        };

        Self {
            config: Mutex::new(config),
        }
    }

    pub fn build(self: Arc<Self>) -> Features {
        self.config.lock().clone()
    }

    pub fn enable_lana(self: Arc<Self>, event_path: String, prod: bool) -> Arc<Self> {
        self.config.lock().lana = Some(FeatureLana { event_path, prod });
        self
    }

    pub fn enable_nurse(self: Arc<Self>) -> Arc<Self> {
        self.config.lock().nurse = Some(default());
        self
    }

    pub fn enable_direct(self: Arc<Self>) -> Arc<Self> {
        self.config.lock().direct = Some(default());
        self
    }

    pub fn enable_firewall_connection_reset(self: Arc<Self>) -> Arc<Self> {
        self.config.lock().firewall.neptun_reset_conns = true;
        self
    }

    pub fn enable_battery_saving_defaults(self: Arc<Self>) -> Arc<Self> {
        {
            let mut cfg = self.config.lock();
            cfg.wireguard.persistent_keepalive.vpn = Some(115);
            cfg.wireguard.persistent_keepalive.direct = 10;
            cfg.wireguard.persistent_keepalive.proxying = Some(125);
            cfg.wireguard.persistent_keepalive.stun = Some(125);

            let prev = cfg.derp.as_ref().cloned().unwrap_or_default();
            cfg.derp = Some(FeatureDerp {
                tcp_keepalive: Some(125),
                derp_keepalive: Some(125),
                enable_polling: Some(true),
                ..prev
            });
        }
        self
    }

    pub fn enable_validate_keys(self: Arc<Self>) -> Arc<Self> {
        self.config.lock().validate_keys = FeatureValidateKeys(true);
        self
    }

    pub fn enable_ipv6(self: Arc<Self>) -> Arc<Self> {
        self.config.lock().ipv6 = true;
        self
    }

    pub fn enable_nicknames(self: Arc<Self>) -> Arc<Self> {
        self.config.lock().nicknames = true;
        self
    }

    pub fn enable_flush_events_on_stop_timeout_seconds(self: Arc<Self>) -> Arc<Self> {
        self.config.lock().flush_events_on_stop_timeout_seconds = Some(0);
        self
    }

    pub fn enable_link_detection(self: Arc<Self>) -> Arc<Self> {
        self.config.lock().link_detection = Some(default());
        self
    }

    pub fn enable_multicast(self: Arc<Self>) -> Arc<Self> {
        self.config.lock().multicast = true;
        self
    }

    pub fn enable_batching(self: Arc<Self>) -> Arc<Self> {
        self.config.lock().batching = Some(default());
        self
    }

    pub fn enable_dynamic_wg_nt_control(self: Arc<Self>) -> Arc<Self> {
        self.config.lock().wireguard.enable_dynamic_wg_nt_control = true;
        self
    }

    pub fn set_skt_buffer_size(self: Arc<Self>, skt_buffer_size: u32) -> Arc<Self> {
        self.config.lock().wireguard.skt_buffer_size = Some(skt_buffer_size);
        self
    }
}

impl Default for FeaturesDefaultsBuilder {
    fn default() -> Self {
        Self::new()
    }
}

fn default<T: Default>() -> T {
    T::default()
}

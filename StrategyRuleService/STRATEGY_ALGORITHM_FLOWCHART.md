# Strategy Algoritması Flowchart

## Ana Akış Diyagramı

```mermaid
flowchart TD
    Start([Strateji Başlatıldı]) --> Step0[Step 0: TimeCheckRule]
    
    Step0 --> CheckTime{Piyasa Açık mı?<br/>10:00 - 17:59}
    
    CheckTime -->|Hayır| MarketClosed[Piyasa Kapalı]
    MarketClosed --> End1([Strateji Sonlandı<br/>Step = -1])
    
    CheckTime -->|Evet| Step1[Step 1: PortfolioCheckRule]
    
    Step1 --> CheckPortfolio{Hisse Portföyde<br/>Var mı?}
    
    CheckPortfolio -->|Evet| Step2[Step 2: SellRule<br/>Satış Kontrolü]
    CheckPortfolio -->|Hayır| Step3[Step 3: BuyRule<br/>Alım Kontrolü]
    
    %% SellRule Akışı
    Step2 --> CheckBuyPrice{Alış Fiyatı<br/>Var mı?}
    CheckBuyPrice -->|Hayır| NoSell1[Satış Yapılamaz<br/>NO_SELL]
    NoSell1 --> End2([Strateji Sonlandı<br/>Step = -1])
    
    CheckBuyPrice -->|Evet| CalculatePrices[Hesapla:<br/>StopLossPrice<br/>TakeProfitPrice]
    
    CalculatePrices --> CheckTakeProfit{Mevcut Fiyat >=<br/>Take Profit?}
    
    CheckTakeProfit -->|Evet| TakeProfitSell[Take Profit Tetiklendi<br/>SATIŞ YAP]
    TakeProfitSell --> SendSellOrder[TradeService'e<br/>Satış Emri Gönder]
    SendSellOrder --> End3([Strateji Sonlandı<br/>Step = -1])
    
    CheckTakeProfit -->|Hayır| CheckStopLoss{Mevcut Fiyat <=<br/>Stop Loss?}
    
    CheckStopLoss -->|Evet| StopLossSell[Stop Loss Tetiklendi<br/>ZARAR KESME SATIŞI]
    StopLossSell --> SendStopLossOrder[TradeService'e<br/>Satış Emri Gönder]
    SendStopLossOrder --> End4([Strateji Sonlandı<br/>Step = -1])
    
    CheckStopLoss -->|Hayır| WaitSell[Bekle<br/>Satış Şartları Oluşmadı<br/>NO_SELL]
    WaitSell --> End5([Strateji Sonlandı<br/>Step = -1])
    
    %% BuyRule Akışı
    Step3 --> CheckOpeningPrice{Açılış Fiyatı<br/>Var mı?}
    CheckOpeningPrice -->|Hayır| NoBuy1[Alım Yapılamaz<br/>NO_DATA]
    NoBuy1 --> End6([Strateji Sonlandı<br/>Step = -1])
    
    CheckOpeningPrice -->|Evet| CalculateEntryPrice[Hesapla Entry Fiyatı:<br/>OpeningPrice * 1 + EntryThreshold%]
    
    CalculateEntryPrice --> CheckEntryPrice{Mevcut Fiyat <=<br/>Entry Fiyatı?}
    
    CheckEntryPrice -->|Hayır| WaitBuy[Bekle<br/>Entry Fiyatına Ulaşılmadı<br/>NO_BUY]
    WaitBuy --> End7([Strateji Sonlandı<br/>Step = -1])
    
    CheckEntryPrice -->|Evet| CheckBalance{Bakiye Yeterli<br/>mi?}
    
    CheckBalance -->|Hayır| InsufficientBalance[Yetersiz Bakiye<br/>BUY_INSUFFICIENT_BALANCE]
    InsufficientBalance --> End8([Strateji Sonlandı<br/>Step = -1])
    
    CheckBalance -->|Evet| SendBuyOrder[TradeService'e<br/>Alım Emri Gönder]
    
    SendBuyOrder --> CheckTradeResponse{Trade Başarılı<br/>mı?}
    
    CheckTradeResponse -->|Evet| BuySuccess[Alım Başarılı<br/>BUY]
    CheckTradeResponse -->|Hayır| BuyFailed[Alım Başarısız<br/>BUY_FAILED]
    
    BuySuccess --> End9([Strateji Sonlandı<br/>Step = -1])
    BuyFailed --> End10([Strateji Sonlandı<br/>Step = -1])
    
    %% Stil
    classDef stepClass fill:#e1f5ff,stroke:#01579b,stroke-width:2px
    classDef decisionClass fill:#fff3e0,stroke:#e65100,stroke-width:2px
    classDef actionClass fill:#e8f5e9,stroke:#2e7d32,stroke-width:2px
    classDef endClass fill:#fce4ec,stroke:#880e4f,stroke-width:2px
    
    class Step0,Step1,Step2,Step3 stepClass
    class CheckTime,CheckPortfolio,CheckBuyPrice,CheckTakeProfit,CheckStopLoss,CheckOpeningPrice,CheckEntryPrice,CheckBalance,CheckTradeResponse decisionClass
    class TakeProfitSell,StopLossSell,SendSellOrder,SendStopLossOrder,SendBuyOrder,BuySuccess actionClass
    class End1,End2,End3,End4,End5,End6,End7,End8,End9,End10 endClass
```

## Detaylı Kural Akış Diyagramı

```mermaid
flowchart LR
    subgraph NRulesService["NRulesService - Ana Motor"]
        A[ProcessRulesAsync] --> B[Her Strateji İçin]
        B --> C[ProcessStrategyAsync]
        C --> D[UpdateContextAsync<br/>Piyasa Verilerini Güncelle]
        D --> E[Session.Fire<br/>Kuralları Tetikle]
        E --> F{Kural Tetiklendi<br/>mi?}
        F -->|Evet| G[Fact'leri Güncelle<br/>session.Update]
        F -->|Hayır| H[Döngüden Çık]
        G --> I{Max Iterasyon<br/>10'a ulaşıldı mı?}
        I -->|Hayır| E
        I -->|Evet| J[SaveStrategyEventsAsync]
        J --> K[RabbitMQ'ya<br/>Notification Gönder]
        K --> H
    end
    
    subgraph Rules["Kurallar (NRules)"]
        R1[TimeCheckRule<br/>Step 0]
        R2[PortfolioCheckRule<br/>Step 1]
        R3[SellRule<br/>Step 2]
        R4[BuyRule<br/>Step 3]
    end
    
    subgraph Services["Dış Servisler"]
        S1[MarketDataService<br/>Fiyat Verileri]
        S2[PortfolioService<br/>Portföy Kontrolü]
        S3[TradeService<br/>Alım/Satım İşlemleri]
        S4[AccountService<br/>Bakiye Kontrolü]
    end
    
    D -.-> S1
    R2 -.-> S2
    R3 -.-> S3
    R3 -.-> S4
    R4 -.-> S3
    R4 -.-> S4
    
    E --> R1
    R1 --> R2
    R2 --> R3
    R2 --> R4
```

## Step Bazlı Detaylı Akış

```mermaid
stateDiagram-v2
    [*] --> Step0: Strateji Başlat
    
    Step0: Step 0<br/>TimeCheckRule<br/>Piyasa Saati Kontrolü
    Step0 --> Step1: Piyasa Açık (10:00-17:59)
    Step0 --> End: Piyasa Kapalı
    
    Step1: Step 1<br/>PortfolioCheckRule<br/>Portföy Kontrolü
    Step1 --> Step2: Hisse Portföyde Var
    Step1 --> Step3: Hisse Portföyde Yok
    
    Step2: Step 2<br/>SellRule<br/>Satış Kontrolü
    Step2 --> CheckTakeProfit: Alış Fiyatı Var
    Step2 --> End: Alış Fiyatı Yok
    
    CheckTakeProfit: Take Profit Kontrolü
    CheckTakeProfit --> Sell: Fiyat >= Take Profit
    CheckTakeProfit --> CheckStopLoss: Fiyat < Take Profit
    
    CheckStopLoss: Stop Loss Kontrolü
    CheckStopLoss --> Sell: Fiyat <= Stop Loss
    CheckStopLoss --> End: Bekle
    
    Step3: Step 3<br/>BuyRule<br/>Alım Kontrolü
    Step3 --> CheckEntry: Açılış Fiyatı Var
    Step3 --> End: Açılış Fiyatı Yok
    
    CheckEntry: Entry Fiyatı Kontrolü
    CheckEntry --> CheckBalance: Fiyat <= Entry Fiyatı
    CheckEntry --> End: Fiyat > Entry Fiyatı
    
    CheckBalance: Bakiye Kontrolü
    CheckBalance --> Buy: Bakiye Yeterli
    CheckBalance --> End: Bakiye Yetersiz
    
    Buy: Alım İşlemi<br/>TradeService
    Buy --> End: İşlem Tamamlandı
    
    Sell: Satış İşlemi<br/>TradeService
    Sell --> End: İşlem Tamamlandı
    
    End: Step -1<br/>Strateji Sonlandı
    End --> [*]
```

## Veri Akış Diyagramı

```mermaid
flowchart TD
    subgraph Input["Giriş Verileri"]
        I1[Strategy Entity<br/>UserId, StockSymbol, etc.]
        I2[UserPreference<br/>StopLoss%, TakeProfit%, etc.]
        I3[Market Data<br/>CurrentPrice, OpeningPrice]
    end
    
    subgraph Processing["İşleme"]
        P1[StockWorkflow<br/>Oluştur]
        P2[NRules Session<br/>Insert Facts]
        P3[Kuralları Tetikle]
        P4[Step Güncelle]
    end
    
    subgraph Output["Çıkış"]
        O1[StrategyEvent<br/>Kaydet]
        O2[Trade Request<br/>Gönder]
        O3[Notification<br/>RabbitMQ]
    end
    
    I1 --> P1
    I2 --> P1
    I3 --> P1
    
    P1 --> P2
    P2 --> P3
    P3 --> P4
    P4 --> O1
    P4 --> O2
    O1 --> O3
    O2 --> O3
```

## Kural Tetikleme Mantığı

```mermaid
flowchart TD
    Start([NRulesService.ProcessStrategyAsync]) --> Loop[Iterasyon Döngüsü<br/>Max 10]
    
    Loop --> Update[UpdateContextAsync<br/>Piyasa Verilerini Güncelle]
    Update --> Fire[Session.Fire<br/>Kuralları Tetikle]
    
    Fire --> Rule1{TimeCheckRule<br/>Step == 0?}
    Rule1 -->|Evet| TimeCheck[Zaman Kontrolü<br/>10:00-17:59]
    TimeCheck --> Step1[Step = 1 veya -1]
    
    Fire --> Rule2{PortfolioCheckRule<br/>Step == 1?}
    Rule2 -->|Evet| PortfolioCheck[Portföy Kontrolü]
    PortfolioCheck --> Step2[Step = 2 veya 3]
    
    Fire --> Rule3{SellRule<br/>Step == 2?}
    Rule3 -->|Evet| SellCheck[Satış Kontrolü<br/>Take Profit / Stop Loss]
    SellCheck --> Step3[Step = -1]
    
    Fire --> Rule4{BuyRule<br/>Step == 3?}
    Rule4 -->|Evet| BuyCheck[Alım Kontrolü<br/>Entry Threshold]
    BuyCheck --> Step4[Step = -1]
    
    Step1 --> UpdateFacts[Fact'leri Güncelle<br/>session.Update]
    Step2 --> UpdateFacts
    Step3 --> UpdateFacts
    Step4 --> UpdateFacts
    
    UpdateFacts --> CheckFired{Kural Tetiklendi<br/>mi?}
    CheckFired -->|Evet| Loop
    CheckFired -->|Hayır| SaveEvents[Event'leri Kaydet]
    
    SaveEvents --> Notify[Notification Gönder]
    Notify --> End([Bitti])
```

## Özet Tablo

| Step | Kural | Koşul | Aksiyon | Sonraki Step |
|------|-------|-------|---------|--------------|
| 0 | TimeCheckRule | Piyasa açık mı? (10:00-17:59) | Evet → Step 1<br/>Hayır → Step -1 | 1 veya -1 |
| 1 | PortfolioCheckRule | Hisse portföyde var mı? | Evet → Step 2<br/>Hayır → Step 3 | 2 veya 3 |
| 2 | SellRule | Take Profit veya Stop Loss? | Take Profit → SAT<br/>Stop Loss → SAT<br/>Yok → Bekle | -1 |
| 3 | BuyRule | Entry fiyatına ulaşıldı mı? | Evet + Bakiye yeterli → AL<br/>Hayır → Bekle | -1 |
| -1 | - | - | Strateji sonlandı | - |


# Barem: 12 pattern-uri GoF — **4 creaționale + 4 structurale + 4 comportamentale**

Repartiția respectă clasificarea clasică GoF. La raport/prezentare: **câte un mini-capitol per categorie** (problemă domeniu → pattern → fișiere → captură din app dacă există).

---

## Creaționale (4)

| Pattern | Rol în agenția de turism | Unde în cod |
|--------|---------------------------|-------------|
| **Abstract Factory** | Familii de componente „buget” vs „premium” (pachete coerente ca stil). | `Patterns/Factories/AbstractFactory/ITripComponentFactory.cs`, `BudgetTripFactory.cs`, `PremiumTripFactory.cs` |
| **Factory Method** | Crearea transportului concret (avion, tren, autocar) fără `new` dispersate în UI. | `Patterns/Factories/FactoryMethod/TransportFactory.cs`, `PlaneFactory.cs`, `TrainFactory.cs`, `BusFactory.cs` |
| **Builder (+ Director)** | Construcția pas-cu-pas a unui `TripPackage` complex din `TripRequest` (multe câmpuri, ordine stabilă). | `Patterns/Builders/TripPackageBuilder.cs`, `TripDirector.cs`, `TripCreationService.cs` |
| **Prototype** | Duplicare pachet cu structură ierarhică (zile, activități) fără reconstruire manuală. | `Patterns/Prototypes/IPrototype.cs`, `TripPackage.DeepClone`, `TripDay` / `Activity` — folosit la agent (`AgentViewModel`) |

---

## Structurale (4)

| Pattern | Rol în agenția de turism | Unde în cod |
|--------|---------------------------|-------------|
| **Adapter** | Integrare API-uri externe (GeoDB, SerpAPI) la tipurile și fluxurile interne. | `Patterns/Adapters/GeoDb/GeoDbLocationAdapter.cs`, `Patterns/Adapters/SerpApi/SerpApiHotelAdapter.cs` |
| **Decorator** | Compunere dinamică a prețului pachetului cu extra-uri (transfer, asigurare, …). | `Patterns/Decorator/ITripComponent.cs`, `TripDecorator.cs`, `InsuranceDecorator.cs`, … — flux rezervare client |
| **Facade** | Un singur punct de intrare pentru crearea/validarea pachetului în spatele mai multor subsisteme. | `Patterns/Facades/TravelPackageFacade.cs` — `CreatePackageWindow` / `QuickCreatePackageWindow` |
| **Proxy** | Control acces: același contract `IBookingAccessService`, dar filtrare și drepturi după rol (client vs agent). | `Services/BookingAccessProxy.cs`, `IBookingAccessService.cs` — teste în `BookingAccessProxyTests.cs` |

---

## Comportamentale (4)

| Pattern | Rol în agenția de turism | Unde în cod |
|--------|---------------------------|-------------|
| **Chain of Responsibility** | Verificări în lanț înainte de aprobare rezervare (client, pachet, status, locuri, preț). | `Services/BookingApprovalChainFactory.cs`, `Patterns/ChainOfResponsibility/*` — teste în `BookingApprovalChainTests.cs` |
| **Observer** | Notificare UI când se schimbă starea unei rezervări (fără polling). | `Patterns/Observer/BookingNotificationService.cs`, `IBookingObserver.cs`, `ClientViewModel.Update` |
| **State** | Tranziții explicite între stări de rezervare (pending / confirmat / respins / anulat). | `Patterns/State/*`, `Booking.SetState` / `RestoreStateFromStatusName` |
| **Strategy** | Algoritmi înlocuibili de calcul preț (standard, discount, TVA complet). | `Patterns/Strategy/IPricingStrategy.cs`, `PricingContext.cs`, `FullPricingStrategy.cs` — teste în `PricingStrategyTests.cs` |

---

## Pattern-uri suplimentare (în proiect, dar **nu** în cele 12 de mai sus)

Utile la **bonus / întrebări** la oral, fără a încălca baremul „4+4+4”:

- **Flyweight** — `PackageSharedInfo` / factory (date partajate destinație).
- **Bridge** — `ITravelItinerary`, `TripCreationService` (itinerariu decuplat de transport/cazare).
- **Composite + Visitor** — `IExtraServiceComponent`, `ExtraServiceGroup` / `ExtraServiceLeaf`, `ExtraServiceSummaryVisitor`.
- **Iterator** — `BookingCollection`, iteratori pe status.
- **Memento** — `AdminAnalyticsMemento`, `AdminAnalyticsHistory` (admin).
- **Mediator** — `IMediator` / `AppMediator` (navigare login/logout în WPF).

---

## SOLID + teste (puncte separate la barem)

- **SRP**: `BookingUpdateNotificationFactory`, `BookingStatusDisplay` — text notificări separat de `ClientViewModel`.
- **Teste**: `TravelAgency.Core.Tests` — `dotnet test TravelAgencySystem.sln`.

```bash
dotnet test TravelAgencySystem.sln
dotnet build TravelAgencySystem.sln -c Release
```

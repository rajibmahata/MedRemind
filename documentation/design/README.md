# ?? MedRemind Architecture & Design Documentation

This folder contains comprehensive architecture diagrams, design documents, and flow charts to help understand the MedRemind application structure.

---

## ?? Documents

### Core Architecture
1. **[SYSTEM_ARCHITECTURE_OVERVIEW.md](SYSTEM_ARCHITECTURE_OVERVIEW.md)**
   - Complete system architecture
   - Component relationships
   - Technology stack
   - Deployment architecture

2. **[DATA_FLOW_DIAGRAMS.md](DATA_FLOW_DIAGRAMS.md)**
   - Prescription upload flow
   - OCR processing flow
   - AI/LLM processing pipeline
   - Medicine validation flow

3. **[COMPONENT_INTERACTION_DIAGRAMS.md](COMPONENT_INTERACTION_DIAGRAMS.md)**
   - Backend components
   - Mobile components
   - Python middleware integration
   - Database interactions

4. **[IMPLEMENTATION_STATUS.md](IMPLEMENTATION_STATUS.md)**
   - What's completed ?
   - What's in progress ??
   - What's missing ?
   - Future enhancements ??

### Application Flows
5. **[PRESCRIPTION_PROCESSING_FLOW.md](PRESCRIPTION_PROCESSING_FLOW.md)**
   - End-to-end prescription processing
   - Multi-LLM orchestration
   - Validation and safety checks
   - Storage and caching

6. **[USER_JOURNEY_FLOWS.md](USER_JOURNEY_FLOWS.md)**
   - User authentication flow
   - Medication management flow
   - Reminder scheduling flow
   - Adherence tracking flow

### Technical Diagrams
7. **[DATABASE_SCHEMA_DIAGRAM.md](DATABASE_SCHEMA_DIAGRAM.md)**
   - Entity relationships
   - Table structures
   - Indexes and constraints

8. **[API_ARCHITECTURE_DIAGRAM.md](API_ARCHITECTURE_DIAGRAM.md)**
   - API endpoints
   - Controller structure
   - Service layer architecture
   - Middleware pipeline

---

## ?? How to Read the Diagrams

### Symbols Used
- ? **Completed** - Feature is fully implemented and tested
- ?? **In Progress** - Feature is partially implemented
- ? **Missing** - Feature is not yet implemented
- ?? **Planned** - Feature is planned for future

### Diagram Types
- **Flowcharts** - Show process flows (Mermaid diagrams)
- **Component Diagrams** - Show system components
- **Sequence Diagrams** - Show interaction sequences
- **Entity Diagrams** - Show database relationships

---

## ?? Quick Start

### For Developers
1. Start with **SYSTEM_ARCHITECTURE_OVERVIEW.md** to understand the big picture
2. Review **IMPLEMENTATION_STATUS.md** to see what's done
3. Check specific flow diagrams for detailed understanding

### For Designers
1. Review **USER_JOURNEY_FLOWS.md** for UX/UI context
2. Check **COMPONENT_INTERACTION_DIAGRAMS.md** for component relationships

### For DevOps
1. See **SYSTEM_ARCHITECTURE_OVERVIEW.md** for deployment architecture
2. Review **API_ARCHITECTURE_DIAGRAM.md** for infrastructure needs

---

## ?? Architecture at a Glance

```
???????????????????????????????????????????????????????????????
?                     MedRemind System                        ?
???????????????????????????????????????????????????????????????
?  Mobile App     ?   Backend API     ?  Python Middleware    ?
?  (.NET MAUI)    ?   (.NET 10)       ?  (FastAPI + CrewAI)   ?
?                 ?                   ?                       ?
?  ? Complete    ?   ? Complete     ?   ? Complete         ?
???????????????????????????????????????????????????????????????
         ?                   ?                    ?
         ??????????????????????????????????????????
                             ?
                    ???????????????????
                    ?   SQLite DB     ?
                    ?   ? Complete   ?
                    ???????????????????
```

---

## ?? Related Documentation

- **Backend Docs**: `backend/MedRemind.API/Docs/`
- **Mobile Docs**: `mobile/MedRemind.Mobile/Docs/`
- **Python Docs**: `python-microservice/`
- **Test Docs**: `backend/MedRemind.Tests/Docs/`

---

**Last Updated**: 2026-02-02  
**Status**: ?? Active Development  
**Version**: 1.0.0
